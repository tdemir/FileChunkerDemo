using FileChunkerDemo.Enums;
using FileChunkerDemo.Models;

namespace FileChunkerDemo.Helpers;

public interface IFileProcessor
{
    Task<CustomFile> CreateCustomFile(string fullPathFileName, HashingAlgoTypes hashingAlgoType);
    Task<List<CustomFileChunk>> CreateCustomFileChunks(CustomFile file);
    void DeleteUploadedLocalFiles(CustomFile file);
    Task<bool> SplitFile(string fullPathFileName, CustomFile customFile);
    Task UploadFileChunks(CustomFile customFile);

    Task<bool> MergeFileAsync(string destinationPath, CustomFile file);
    bool VerifyChecksum(CustomFile file, string compareFullPathFileName);
}