using FileChunkerDemo.Config;
using FileChunkerDemo.Data;
using FileChunkerDemo.Helpers;
using FileChunkerDemo.Repositories;
using FileChunkerDemo.Repositories.Interfaces;
using FileChunkerDemo.StorageProviderServices;
using FileChunkerDemo.StorageProviderServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace FileChunkerDemo;

public class Program
{
    public static void Main(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        
        Log.Logger.Debug("Starting console app...");
        
        // Set up DI container
        ServiceCollection services = new();

// Register Serilog as the logging provider
        services.AddLogging(builder =>
        {
            builder.AddSerilog(Log.Logger);
        });

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        
        var appSettings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(appSettings);
        services.AddSingleton<AppSettings>(appSettings);
        services.AddSingleton<AsyncLock>();

        services.AddSingleton<IFileProcessor, FileProcessor>();
        services.AddSingleton<ICustomFileRepository, CustomFileRepository>();
        services.AddSingleton<ICustomFileChunkRepository, CustomFileChunkRepository>();
        services.AddSingleton<IStoredFilesRepository, StoredFilesRepository>();

// Register your main application class
        services.AddKeyedSingleton<IStorageProvider, DatabaseStorageProvider>("DatabaseStorageProvider");
        services.AddKeyedSingleton<IStorageProvider, FileSystemStorageProvider>("FileSystemStorageProvider");
        services.AddSingleton<Application>();
        services.AddSingleton<FileProcessor>();

// Add IConfiguration to DI
        services.AddSingleton<IConfiguration>(configuration);

// Build the provider
        var provider = services.BuildServiceProvider();

// Run the app
        var app = provider.GetRequiredService<Application>();
        try
        {
            app.Run().Wait();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Unhandled exception");
        }

        Log.Logger.Debug("Ending console app...");
        Log.CloseAndFlush();

    }
}