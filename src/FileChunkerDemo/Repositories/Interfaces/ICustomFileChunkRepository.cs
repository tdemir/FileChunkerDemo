using FileChunkerDemo.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileChunkerDemo.Repositories.Interfaces
{
    public interface ICustomFileChunkRepository
    {
        Task<CustomFileChunk> GetByIdAsync(int id);
        Task<IEnumerable<CustomFileChunk>> GetByFileIdAsync(int fileId);
        Task<CustomFileChunk> InsertAsync(CustomFileChunk entity);
        Task<List<CustomFileChunk>> InsertBulkAsync(List<CustomFileChunk> entityList);
        Task<CustomFileChunk> UpdateAsync(CustomFileChunk entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}