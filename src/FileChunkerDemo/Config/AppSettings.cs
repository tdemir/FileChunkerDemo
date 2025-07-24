namespace FileChunkerDemo.Config;

public class AppSettings
{
    public string DefaultHashAlgorithm { get; set; }
    public Enums.HashingAlgoTypes DefaultHashAlgorithmEnum
    {
        get
        {
            var defaultHashAlgoritm =
                (Enums.HashingAlgoTypes)Enum.Parse(typeof(Enums.HashingAlgoTypes), DefaultHashAlgorithm,
                    true);
            return defaultHashAlgoritm;
        }
    }
    public int MaxChunkSizeInBytes { get; set; }
    public string FileProcessFolder { get; set; }

    public string FileProcessFolderFullPath
    {
        get
        {
            return Path.Combine(AppContext.BaseDirectory, FileProcessFolder);
        }
    }

    public string[] ActiveStorages { get; set; }


    

}