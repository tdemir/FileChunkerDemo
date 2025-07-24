using FileChunkerDemo.Models;
using FileChunkerDemo.Repositories.Interfaces;
using FileChunkerDemo.StorageProviderServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileChunkerDemo.StorageProviderServices;

public class DatabaseStorageProvider : IStorageProvider
{
    private readonly ILogger<DatabaseStorageProvider> _logger;
    private readonly IStoredFilesRepository _storedFilesRepository;

    public DatabaseStorageProvider(ILogger<DatabaseStorageProvider> logger,
        IStoredFilesRepository storedFilesRepository)
    {
        _logger = logger;
        _storedFilesRepository = storedFilesRepository;
    }

    public async Task UploadFile(string filename, byte[] content)
    {
        try
        {
            _logger.LogInformation($"Uploading file: {filename}");
            var sf = new StoredFile();
            sf.FileName = filename;
            sf.Content = GetBase64Value(content);
            sf.CreateDate = DateTime.UtcNow;
            await _storedFilesRepository.InsertAsync(sf);
            _logger.LogInformation($"Uploaded file: {filename}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to upload file: {filename}");
            throw;
        }
    }

    public async Task<byte[]> DownloadFile(string filename)
    {
        try
        {
            _logger.LogInformation($"Downloading file: {filename}");
            var sf = await _storedFilesRepository.GetByFileNameAsync(filename);
            if (sf == null)
            {
                return null;
            }
            var value = GetBytes(sf.Content);
            _logger.LogInformation($"Downloaded file: {filename}");
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to download file: {filename}");
            throw;
        }
    }

    public async Task CleanUp()
    {
        await _storedFilesRepository.DeleteAllAsync();
    }

    private string GetBase64Value(byte[] content)
    {
        return Convert.ToBase64String(content);
    }

    private byte[] GetBytes(string content)
    {
        return Convert.FromBase64String(content);
    }
}