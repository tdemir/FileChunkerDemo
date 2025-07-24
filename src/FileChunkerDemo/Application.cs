using FileChunkerDemo.Enums;
using FileChunkerDemo.Helpers;
using FileChunkerDemo.Repositories.Interfaces;
using FileChunkerDemo.StorageProviderServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileChunkerDemo;

public class Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Application> _logger;
    private readonly Config.AppSettings _appSettings;
    private readonly IFileProcessor _fileProcessor;
    private readonly ICustomFileRepository _customFileRepository;
    private readonly ICustomFileChunkRepository _customFileChunkRepository;

    public Application(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<Application>>();
        _appSettings = _serviceProvider.GetRequiredService<Config.AppSettings>();
        _fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();
        _customFileRepository = _serviceProvider.GetRequiredService<ICustomFileRepository>();
        _customFileChunkRepository = _serviceProvider.GetRequiredService<ICustomFileChunkRepository>();
    }

    static SemaphoreSlim _semaphore = new SemaphoreSlim(3, 3);

    private async Task ProcessSplitFile(string fileFullPath)
    {
        await _semaphore.WaitAsync();

        try
        {
            _logger.LogInformation($"Processing file (start): {fileFullPath} " + DateTime.UtcNow);

            var file = await _fileProcessor.CreateCustomFile(fileFullPath, _appSettings.DefaultHashAlgorithmEnum);
            await _customFileRepository.InsertAsync(file);
            //split file
            //before split set status
            file.FileProcessStatusEnum = FileProcessStatus.Processing;
            await _customFileRepository.UpdateAsync(file);

            try
            {
                await _fileProcessor.SplitFile(fileFullPath, file);
                await _customFileRepository.UpdateAsync(file); //update number of chunks file
            }
            catch (Exception)
            {
                file.FileProcessStatusEnum = FileProcessStatus.Failed;
                await _customFileRepository.UpdateAsync(file);
            }


            if (file.FileProcessStatusEnum != FileProcessStatus.Failed)
            {
                try
                {
                    var chunks = await _fileProcessor.CreateCustomFileChunks(file);
                    await _customFileChunkRepository.InsertBulkAsync(chunks);
                }
                catch (Exception)
                {
                    file.FileProcessStatusEnum = FileProcessStatus.Failed;
                    await _customFileRepository.UpdateAsync(file);
                }
            }

            if (file.FileProcessStatusEnum != FileProcessStatus.Failed)
            {
                //upload files
                try
                {
                    await _fileProcessor.UploadFileChunks(file);

                    file.FileProcessStatusEnum =
                        file.FileChunks.Any(x => x.FileProcessStatusEnum == FileProcessStatus.Failed) 
                            ? FileProcessStatus.Failed 
                            : FileProcessStatus.Completed;
                }
                catch (Exception e)
                {
                    _logger.LogError(e,$"File chunk processing failed. FileId: {file.Id}");
                    file.FileProcessStatusEnum = FileProcessStatus.Failed;
                }
                foreach (var chunk in file.FileChunks)
                    await _customFileChunkRepository.UpdateAsync(chunk);

                await _customFileRepository.UpdateAsync(file);
            }

            if (file.FileProcessStatusEnum != FileProcessStatus.Failed)
            {
                //delete splitted files
                _fileProcessor.DeleteUploadedLocalFiles(file);
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _logger.LogInformation($"Processing file (end): {fileFullPath} " + DateTime.UtcNow);
            _semaphore.Release();
        }
    }

    private async Task ProcessSplitFiles(List<string> files)
    {
        _logger.LogInformation("Splitting files...");
        var listTasks = new List<Task>();
        foreach (var file in files)
        {
            listTasks.Add(ProcessSplitFile(file));
        }

        if (listTasks.Any())
            await Task.WhenAll(listTasks);
    }

    private async Task ProcessMergeFile(int fileId, string destinationFolderPath)
    {
        _logger.LogInformation($"Processing merge file (start): {fileId} " + DateTime.UtcNow);
        var file = await _customFileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException($"File not found. FileId : {fileId}");
        }

        file.FileChunks = (await _customFileChunkRepository.GetByFileIdAsync(fileId)).ToList();

        if (!await _fileProcessor.MergeFileAsync(destinationFolderPath, file))
        {
            throw new Exception($"File merge failed. FileId : {fileId}");
        }

        var newFile = Path.Combine(destinationFolderPath, file.FileName);
        if (!_fileProcessor.VerifyChecksum(file, newFile))
        {
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }

            throw new Exception($"File verification failed. FileId : {fileId}");
        }

        _logger.LogInformation($"Processing merge file (end): {fileId} " + DateTime.UtcNow);
    }

    private async Task CleanUpStorages()
    {
        _logger.LogInformation("Cleaning up storages");
        foreach (var activeStorage in _appSettings.ActiveStorages)
        {
            var storage = _serviceProvider.GetKeyedService<IStorageProvider>(activeStorage);
            await storage.CleanUp();
        }
    }

    public async Task Run()
    {
        //await CleanUpStorages();

        _logger.LogInformation("Processing files...");

        var splitFilesBasePath = $"/Users/{Environment.UserName}/Downloads";

        var splitFileList = new List<string>
        {
            Path.Combine(splitFilesBasePath, "CERTIFICATE_coursera_ibm_nodejs.jpeg"),
            Path.Combine(splitFilesBasePath, "unboxing-net-aspi-build-observe-deploy.pdf")
        };

        //split and upload
        await ProcessSplitFiles(splitFileList);

        //download and merge
        var destinationFolderPath = _appSettings.FileProcessFolderFullPath;
        await ProcessMergeFile(60, destinationFolderPath);
        await ProcessMergeFile(61, destinationFolderPath);


        Console.WriteLine($"Press any key to exit...");
        Console.ReadLine();
    }
}