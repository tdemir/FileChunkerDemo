using FileChunkerDemo.Data;
using FileChunkerDemo.Helpers;
using FileChunkerDemo.Models;
using FileChunkerDemo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileChunkerDemo.Repositories;

public class StoredFilesRepository : IStoredFilesRepository
{
    private readonly AppDbContext _context;
    private readonly AsyncLock _asyncLock;

    public StoredFilesRepository(AppDbContext context, AsyncLock asyncLock)
    {
        _context = context;
        _asyncLock = asyncLock;
    }

    public async Task<StoredFile> GetByIdAsync(int id)
    {
        using (await _asyncLock.LockAsync())
        {
            return await _context.StoredFiles.FindAsync(id);
        }
    }

    public async Task<StoredFile> GetByFileNameAsync(string fileName)
    {
        using (await _asyncLock.LockAsync())
        {
            return await _context.StoredFiles
                .FirstOrDefaultAsync(x => x.FileName == fileName);
        }
    }

    public async Task<StoredFile> InsertAsync(StoredFile entity)
    {
        using (await _asyncLock.LockAsync())
        {
            await _context.StoredFiles.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }

    public async Task<StoredFile> UpdateAsync(StoredFile entity)
    {
        using (await _asyncLock.LockAsync())
        {
            _context.StoredFiles.Update(entity);
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
                _context.StoredFiles.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteAllAsync()
    {
        using (await _asyncLock.LockAsync())
        {
            await _context.StoredFiles.ExecuteDeleteAsync();
        }
    }
}