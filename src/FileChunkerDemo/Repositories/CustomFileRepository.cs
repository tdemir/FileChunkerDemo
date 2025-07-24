using FileChunkerDemo.Data;
using FileChunkerDemo.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using FileChunkerDemo.Helpers;
using FileChunkerDemo.Repositories.Interfaces;

namespace FileChunkerDemo.Repositories
{
    public class CustomFileRepository : ICustomFileRepository
    {
        private readonly AppDbContext _context;
        private readonly AsyncLock _asyncLock;

        public CustomFileRepository(AppDbContext context, AsyncLock asyncLock)
        {
            _context = context;
            _asyncLock = asyncLock;
        }

        public async Task<CustomFile> GetByIdAsync(int id)
        {
            using (await _asyncLock.LockAsync())
            {
                return await _context.CustomFiles.FindAsync(id);
            }
        }

        public async Task<CustomFile> GetByUniqueIdentifierAsync(string uniqueIdentifier)
        {
            using (await _asyncLock.LockAsync())
            {
                return await _context.CustomFiles
                    .FirstOrDefaultAsync(x => x.UniqueIdentifier == uniqueIdentifier);
            }
        }

        public async Task<CustomFile> InsertAsync(CustomFile entity)
        {
            using (await _asyncLock.LockAsync())
            {
                await _context.CustomFiles.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
        }

        public async Task<CustomFile> UpdateAsync(CustomFile entity)
        {
            using (await _asyncLock.LockAsync())
            {
                entity.LastUpdateDate = DateTime.UtcNow;
                _context.CustomFiles.Update(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using (await _asyncLock.LockAsync())
            {
                var entity = await GetByIdAsync(id);
                if (entity != null)
                {
                    _context.CustomFiles.Remove(entity);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}