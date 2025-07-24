using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileChunkerDemo.Models;

namespace FileChunkerDemo.Repositories.Interfaces
{
    public interface ICustomFileRepository
    {
        Task<CustomFile> GetByIdAsync(int id);
        Task<CustomFile> GetByUniqueIdentifierAsync(string uniqueIdentifier);
        Task<CustomFile> InsertAsync(CustomFile entity);
        Task<CustomFile> UpdateAsync(CustomFile entity);
        Task DeleteAsync(int id);
    }
}