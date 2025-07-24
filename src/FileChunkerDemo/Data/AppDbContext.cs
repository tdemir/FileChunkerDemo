using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileChunkerDemo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FileChunkerDemo.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        // Constructor for runtime use
        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }
        // Constructor for design-time use
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // {
        //     if (!optionsBuilder.IsConfigured)
        //     {
        //         var configuration = _configuration ?? new ConfigurationBuilder()
        //             .SetBasePath(Directory.GetCurrentDirectory())
        //             .AddJsonFile("appsettings.json")
        //             .AddJsonFile("appsettings.Development.json", optional: true)
        //             .Build();
        //         optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        //     }
        //     base.OnConfiguring(optionsBuilder);
        // }

        public DbSet<CustomFile> CustomFiles { get; set; }
        public DbSet<CustomFileChunk> CustomFileChunks { get; set; }

        public DbSet<StoredFile> StoredFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomFile>()
                .HasIndex(u => u.UniqueIdentifier)
                .IsUnique();

            modelBuilder.Entity<StoredFile>()
                .HasIndex(u => u.FileName)
                .IsUnique();
        }
    }
}