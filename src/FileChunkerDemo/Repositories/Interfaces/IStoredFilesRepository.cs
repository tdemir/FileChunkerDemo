using FileChunkerDemo.Models;

namespace FileChunkerDemo.Repositories.Interfaces;

public interface IStoredFilesRepository
{
    Task<StoredFile> GetByIdAsync(int id);
    Task<StoredFile> GetByFileNameAsync(string fileName);
    Task<StoredFile> InsertAsync(StoredFile entity);
    Task<StoredFile> UpdateAsync(StoredFile entity);
    Task DeleteAsync(int id);
    Task DeleteAllAsync();
}