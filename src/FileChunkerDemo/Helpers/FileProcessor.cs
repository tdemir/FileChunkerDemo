using FileChunkerDemo.Enums;
using FileChunkerDemo.Models;
using System.Security.Cryptography;
using FileChunkerDemo.Config;
using FileChunkerDemo.Repositories.Interfaces;
using FileChunkerDemo.StorageProviderServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileChunkerDemo.Helpers;

public class FileProcessor : IFileProcessor
{
    private readonly ILogger<FileProcessor> _logger;
    private readonly AppSettings _appSettings;
    private readonly IServiceProvider _serviceProvider;

    private static SemaphoreSlim _semaphoreSplitter = new SemaphoreSlim(2, 2);

    public FileProcessor(ILogger<FileProcessor> logger, AppSettings appSettings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _appSettings = appSettings;

        _serviceProvider = serviceProvider;

        if (!System.IO.Directory.Exists(_appSettings.FileProcessFolderFullPath))
        {
            _logger.LogInformation($"Creating directory {_appSettings.FileProcessFolderFullPath}");
            Directory.CreateDirectory(_appSettings.FileProcessFolderFullPath);
            _logger.LogInformation($"Created directory {_appSettings.FileProcessFolderFullPath}");
        }
    }

    private string GetChecksum(HashingAlgoTypes hashingAlgoType, string filename)
    {
        _logger.LogInformation($"Getting checksum for {hashingAlgoType} of {filename}");
        try
        {
            using (HashAlgorithm hasher = hashingAlgoType switch
                   {
                       HashingAlgoTypes.MD5 => MD5.Create(),
                       HashingAlgoTypes.SHA1 => SHA1.Create(),
                       HashingAlgoTypes.SHA256 => SHA256.Create(),
                       HashingAlgoTypes.SHA384 => SHA384.Create(),
                       HashingAlgoTypes.SHA512 => SHA512.Create(),
                       _ => throw new ArgumentException($"Unsupported hashing algorithm: {hashingAlgoType}")
                   })
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = hasher.ComputeHash(stream);
                    var value = BitConverter.ToString(hash).Replace("-", "");
                    _logger.LogInformation($"Checksum created for {hashingAlgoType} of {filename}");
                    return value;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to get checksum for {filename}. Hashing algorithm: {hashingAlgoType}");
            throw;
        }
    }


    public Task<CustomFile> CreateCustomFile(string fullPathFileName, HashingAlgoTypes hashingAlgoType)
    {
        _logger.LogInformation($"Creating custom file for {fullPathFileName}");
        CustomFile cf = null;
        try
        {
            var fi = new FileInfo(fullPathFileName);

            cf = new()
            {
                UniqueIdentifier = Guid.NewGuid().ToString("D"),
                FileName = fi.Name,
                FileSize = fi.Length,
                FileCreatedDate = fi.CreationTimeUtc,
                FileExtension = fi.Extension,
                Checksum = GetChecksum(hashingAlgoType, fullPathFileName),
                ChecksumAlgorithmEnum = hashingAlgoType,
                FileProcessStatusEnum = FileProcessStatus.Created,
                CreateDate = DateTime.UtcNow
            };
            _logger.LogInformation($"Created custom file for {fullPathFileName}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to create custom file for {fullPathFileName}");
            throw;
        }

        return Task.FromResult(cf);
    }

    public Task<List<CustomFileChunk>> CreateCustomFileChunks(CustomFile file)
    {
        _logger.LogInformation($"Creating custom file chunks for {file.FileName}. FileId: {file.Id}");

        var chunks = new List<CustomFileChunk>();

        try
        {
            var folderPath = Path.Combine(_appSettings.FileProcessFolderFullPath, file.UniqueIdentifier);

            var storageProviders = _appSettings.ActiveStorages;

            for (int i = 0; i < file.NumberOfChunks; i++)
            {
                var fi = new FileInfo(Path.Combine(folderPath, file.GenerateChunkFileName(i)));
                foreach (var storageProvider in storageProviders)
                {
                    var cfc = new CustomFileChunk()
                    {
                        FileId = file.Id,
                        Index = i,
                        FileName = file.GenerateChunkFileName(i),
                        FileSize = fi.Length,
                        CreatedAt = DateTime.UtcNow,
                        FileProcessStatusEnum = FileProcessStatus.Created,
                        StorageProvider = storageProvider,
                        UploadErrorReason = string.Empty
                    };
                    chunks.Add(cfc);
                }
            }

            _logger.LogInformation($"Created custom file chunks for {file.FileName}. FileId: {file.Id}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to create custom file chunks for {file.FileName}. FileId: {file.Id}");
            throw;
        }

        return Task.FromResult(chunks);
    }


    public async Task UploadFileChunks(CustomFile file)
    {
        _logger.LogInformation($"Uploading file chunks. FileId: {file.Id}");

        var uniqueFolderPath = Path.Combine(_appSettings.FileProcessFolderFullPath, file.UniqueIdentifier);
        if (!Directory.Exists(uniqueFolderPath))
        {
            throw new DirectoryNotFoundException($"Directory {uniqueFolderPath} does not exist");
        }

        if (file.NumberOfChunks == 0)
        {
            throw new Exception($"File {file.FileName} has 0 chunks");
        }

        foreach (var chunk in file.FileChunks)
        {
            var fullPathFileName = Path.Combine(uniqueFolderPath, chunk.FileName);
            try
            {
                if (!File.Exists(fullPathFileName))
                {
                    throw new FileNotFoundException($"File {fullPathFileName} does not exist");
                }

                var fileBytes = await File.ReadAllBytesAsync(fullPathFileName);
                var storageProvider = _serviceProvider.GetKeyedService<IStorageProvider>(chunk.StorageProvider);
                await storageProvider.UploadFile(chunk.FileName, fileBytes);
                chunk.FileProcessStatusEnum = FileProcessStatus.Completed;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Failed to upload file chunks for {fullPathFileName}. FileId: {file.Id} FileChunkId: {chunk.Id}");
                chunk.FileProcessStatusEnum = FileProcessStatus.Failed;
                chunk.UploadErrorReason = e.Message;
            }
        }
        _logger.LogInformation($"File chunks upload process completed. FileId: {file.Id}");
    }

    public void DeleteUploadedLocalFiles(CustomFile file)
    {
        _logger.LogInformation($"Deleting uploaded file chunks. FileId: {file.Id}");
        var folderPath = Path.Combine(_appSettings.FileProcessFolderFullPath, file.UniqueIdentifier);
        if (!Directory.Exists(folderPath))
        {
            _logger.LogInformation($"Directory {folderPath} does not exist while deleting uploaded file chunks. FileId: {file.Id}");
            return;
        }
        Directory.Delete(folderPath, true);
        _logger.LogInformation($"Deleted uploaded file chunks. FileId: {file.Id}");
    }

    public async Task<bool> SplitFile(string fullPathFileName, CustomFile file)
    {
        _logger.LogInformation($"Splitting process starting for file {fullPathFileName}. FileId: {file.Id}");
        
        await _semaphoreSplitter.WaitAsync();
        
        _logger.LogInformation($"Splitting process {fullPathFileName}.(Semaphore scope started) FileId: {file.Id}");
        
        string uniqueFolderPath = string.Empty;

        try
        {
            //--------------------------------------------------
            uniqueFolderPath = Path.Combine(_appSettings.FileProcessFolderFullPath, file.UniqueIdentifier);
            if (System.IO.Directory.Exists(uniqueFolderPath))
            {
                _logger.LogInformation($"Directory {uniqueFolderPath} already exists and it will be deleted. FileId: {file.Id}");
                Directory.Delete(uniqueFolderPath, true);
            }

            Directory.CreateDirectory(uniqueFolderPath);


            //--------------------------------------------------
            file.NumberOfChunks = 0;

            using (var fileStream = new FileStream(fullPathFileName, FileMode.Open, FileAccess.Read))
            {
                int index = 0;
                while (fileStream.Position < fileStream.Length)
                {
                    using (var outputFile =
                           new FileStream(
                               Path.Combine(uniqueFolderPath, file.GenerateChunkFileName(index)),
                               FileMode.Create, FileAccess.Write))
                    {
                        int remaining = _appSettings.MaxChunkSizeInBytes, bytesRead;
                        byte[] buffer = new byte[_appSettings.MaxChunkSizeInBytes];
                        while (remaining > 0 &&
                               (bytesRead = fileStream.Read(buffer, 0,
                                   Math.Min(remaining, _appSettings.MaxChunkSizeInBytes))) > 0)
                        {
                            outputFile.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }

                        file.NumberOfChunks++;
                        index++;
                    }
                }
            }
            _logger.LogInformation($"Splitting process completed for file {fullPathFileName}. FileId: {file.Id}");
        }
        catch (Exception e)
        {
            _logger.LogError(e,$"Splitting file chunks. FileId:{file.Id}");

            if (uniqueFolderPath != string.Empty
                && System.IO.Directory.Exists(uniqueFolderPath))
            {
                _logger.LogInformation($"Directory {uniqueFolderPath} is deleting. FileId: {file.Id}");
                
                Directory.Delete(uniqueFolderPath, true);
                
                _logger.LogInformation($"Directory {uniqueFolderPath} is deleted. FileId: {file.Id}");
            }

            throw;
        }
        finally
        {
            _logger.LogInformation($"Splitting process {fullPathFileName}.(Semaphore scope ended) FileId: {file.Id}");
            _semaphoreSplitter.Release();
        }

        return true;
        //splitted
    }

    private async Task DownloadChunks(string uniqueFolderPath, CustomFile file)
    {
        _logger.LogInformation($"Downloading chunks for file {file.FileName}. FileId: {file.Id}");

        foreach (var chunk in file.FileChunks)
        {
            var fullPathFileName = Path.Combine(uniqueFolderPath, chunk.FileName);
            if (File.Exists(fullPathFileName))
            {
                _logger.LogInformation(
                    $"File {fullPathFileName} already exists. FileId: {file.Id} StorageProvider: {chunk.StorageProvider}");
                continue;
            }

            var storageProvider = _serviceProvider.GetKeyedService<IStorageProvider>(chunk.StorageProvider);
            var byteData = await storageProvider.DownloadFile(chunk.FileName);
            if (byteData == null)
            {
                _logger.LogInformation(
                    $"File {chunk.FileName} does not exist in {chunk.StorageProvider}. FileId: {file.Id}");
                continue;
            }

            await File.WriteAllBytesAsync(fullPathFileName, byteData);
            _logger.LogInformation(
                $"File {chunk.FileName} has been downloaded to {fullPathFileName}. FileId: {file.Id} StorageProvider: {chunk.StorageProvider}");
        }

        _logger.LogInformation($"Chunks ({file.FileName}) downloaded to folder {uniqueFolderPath}. FileId: {file.Id}");
    }

    public async Task<bool> MergeFileAsync(string destinationPath, CustomFile file)
    {
        _logger.LogInformation($"Merging file {file.FileName}. DestinationPath: {destinationPath}. FileId: {file.Id}");
        
        var uniqueFolderPath = Path.Combine(_appSettings.FileProcessFolderFullPath, file.UniqueIdentifier);
        if (Directory.Exists(uniqueFolderPath))
        {
            Directory.Delete(uniqueFolderPath, true);
        }

        Directory.CreateDirectory(uniqueFolderPath);


        if (file.NumberOfChunks == 0)
        {
            throw new Exception($"File {file.FileName} has 0 chunks");
        }

        await DownloadChunks(uniqueFolderPath, file);

        var newFilePath = Path.Combine(destinationPath, file.FileName);
        if (File.Exists(newFilePath))
        {
            _logger.LogInformation($"File {newFilePath} already exists. Old file will be deleted. FileId: {file.Id}");
            File.Delete(newFilePath);
            _logger.LogInformation($"File {newFilePath} already exists. Old file were deleted. FileId: {file.Id}");
        }

        try
        {
            using (var outputFile = new FileStream(newFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                for (int i = 0; i < file.NumberOfChunks; i++)
                {
                    var tempFileName = Path.Combine(uniqueFolderPath, file.GenerateChunkFileName(i));

                    int bytesRead = 0;
                    byte[] buffer = new byte[1024];
                    using (var inputTempFile = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        while ((bytesRead = inputTempFile.Read(buffer, 0, 1024)) > 0)
                        {
                            outputFile.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,$"Merging file {file.FileName}. FileId: {file.Id}");
            throw;
        }

        if (Directory.Exists(uniqueFolderPath))
        {
            _logger.LogInformation($"Directory {uniqueFolderPath} exists. Removing file chunks. FileId: {file.Id}");
            Directory.Delete(uniqueFolderPath, true);
            _logger.LogInformation($"Directory {uniqueFolderPath} exists. Removed file chunks. FileId: {file.Id}");
        }

        return true;
    }


    public bool VerifyChecksum(CustomFile file, string compareFullPathFileName)
    {
        _logger.LogInformation($"Verifying checksum for compare file {compareFullPathFileName}. FileId: {file.Id}");
        try
        {
            var newFileChecksum = GetChecksum(file.ChecksumAlgorithmEnum, compareFullPathFileName);
            var value = string.Compare(newFileChecksum, file.Checksum, StringComparison.OrdinalIgnoreCase) == 0;
            _logger.LogInformation($"Verifying checksum completed for compare file {compareFullPathFileName}. FileId: {file.Id}. Result: {value}");
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e,$"Verifying checksum failed for compare file {compareFullPathFileName}. FileId: {file.Id}");
            throw;
        }
    }
}