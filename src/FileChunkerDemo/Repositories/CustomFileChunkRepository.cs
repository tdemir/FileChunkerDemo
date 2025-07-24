using FileChunkerDemo.Data;
using FileChunkerDemo.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FileChunkerDemo.Helpers;
using FileChunkerDemo.Repositories.Interfaces;

namespace FileChunkerDemo.Repositories
{
    public class CustomFileChunkRepository : ICustomFileChunkRepository
    {
        private readonly AppDbContext _context;
        private readonly AsyncLock _asyncLock;

        public CustomFileChunkRepository(AppDbContext context, AsyncLock asyncLock)
        {
            _context = context;
            _asyncLock = asyncLock;
        }

        public async Task<CustomFileChunk> GetByIdAsync(int id)
        {
            using (await _asyncLock.LockAsync())
            {
                return await _context.CustomFileChunks
                    .Include(x => x.CustomFile)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
        }

        public async Task<IEnumerable<CustomFileChunk>> GetByFileIdAsync(int fileId)
        {
            using (await _asyncLock.LockAsync())
            {
                return await _context.CustomFileChunks
                    .Include(x => x.CustomFile)
                    .Where(x => x.FileId == fileId)
                    .OrderBy(x => x.Index)
                    .ToListAsync();
            }
        }

        public async Task<CustomFileChunk> InsertAsync(CustomFileChunk entity)
        {
            using (await _asyncLock.LockAsync())
            {
                await _context.CustomFileChunks.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
        }

        public async Task<List<CustomFileChunk>> InsertBulkAsync(List<CustomFileChunk> entityList)
        {
            using (await _asyncLock.LockAsync())
            {
                await _context.CustomFileChunks.AddRangeAsync(entityList);
                await _context.SaveChangesAsync();
                return entityList;
            }
        }

        public async Task<CustomFileChunk> UpdateAsync(CustomFileChunk entity)
        {
            using (await _asyncLock.LockAsync())
            {
                _context.Entry(entity).State = EntityState.Modified;
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
                    _context.CustomFileChunks.Remove(entity);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using (await _asyncLock.LockAsync())
            {
                return await _context.CustomFileChunks.AnyAsync(x => x.Id == id);
            }
        }
    }
}