using FileChunkerDemo.StorageProviderServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileChunkerDemo.StorageProviderServices;

public class FileSystemStorageProvider : IStorageProvider
{
    private const string STORAGE_FOLDER_NAME = "FILE_SYSTEM_STORAGE";
    
    private readonly ILogger<FileSystemStorageProvider> _logger;
    private readonly string _folderPath;
    

    public FileSystemStorageProvider(ILogger<FileSystemStorageProvider> logger)
    {
        _logger = logger;
        _folderPath = Path.Combine(AppContext.BaseDirectory, STORAGE_FOLDER_NAME);
        if (!Directory.Exists(_folderPath))
        {
            _logger.LogInformation($"Creating directory {_folderPath}");
            Directory.CreateDirectory(_folderPath);
            _logger.LogInformation($"Created directory {_folderPath}");
        }
    }

    public async Task UploadFile(string filename, byte[] content)
    {
        try
        {
            _logger.LogInformation($"Uploading file {filename}");
            await File.WriteAllBytesAsync(Path.Combine(_folderPath, filename), content);
            _logger.LogInformation($"File {filename} uploaded");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to write file {filename}");
            throw;
        }
    }

    public async Task<byte[]> DownloadFile(string filename)
    {
        try
        {
            _logger.LogInformation($"Downloading file {filename}");
            var data = await File.ReadAllBytesAsync(Path.Combine(_folderPath, filename));
            _logger.LogInformation($"File {filename} downloaded");
            return data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to download file: {filename}");
        }
        return null;
    }

    public Task CleanUp()
    {
        if (Directory.Exists(_folderPath))
        {
            Directory.Delete(_folderPath, true);
        }
        Directory.CreateDirectory(_folderPath);
        return Task.CompletedTask;
    }
}