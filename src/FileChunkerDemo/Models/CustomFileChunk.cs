using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileChunkerDemo.Models;

[Table("tbl_file_chunk")]
public class CustomFileChunk
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("file_id")] [Required] public int FileId { get; set; }

    [ForeignKey(nameof(FileId))] public virtual CustomFile CustomFile { get; set; }

    [Column("chunk_index")] [Required] public int Index { get; set; }
    [Column("chunk_name")] [Required] [MaxLength(150)] public string FileName { get; set; }
    [Column("chunk_size")] [Required] public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("storage_provider")] [Required] [MaxLength(50)]
    public string StorageProvider { get; set; }
    
    [Column("file_process_status")] [Required] [MaxLength(50)]
    public string FileProcessStatus { get; set; }

    [NotMapped]
    public Enums.FileProcessStatus FileProcessStatusEnum
    {
        get => (Enums.FileProcessStatus)Enum.Parse(typeof(Enums.FileProcessStatus), FileProcessStatus, true);
        set => FileProcessStatus = value.ToString();
    }
    
    [Column("upload_error_reason")]
    public string UploadErrorReason { get; set; }
}