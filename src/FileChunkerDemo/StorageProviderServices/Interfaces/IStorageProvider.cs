namespace FileChunkerDemo.StorageProviderServices.Interfaces;

public interface IStorageProvider
{
    Task UploadFile(string filename, byte[] content);
    Task<byte[]> DownloadFile(string filename);
    
    Task CleanUp();
}