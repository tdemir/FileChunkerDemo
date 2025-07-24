using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileChunkerDemo.Models;

[Table("tbl_file")]
public class CustomFile
{

    public virtual ICollection<CustomFileChunk> FileChunks { get; set; }
    public CustomFile()
    {
        FileChunks = new HashSet<CustomFileChunk>();
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("unique_identifier")]
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string UniqueIdentifier { get; set; }

    [Column("file_name")]
    [Required]
    [MinLength(3)]
    [MaxLength(150)]
    public string FileName { get; set; }

    [Column("file_size")]
    [Required]
    public long FileSize { get; set; }

    [Column("file_created_date")]
    [Required]
    public DateTime FileCreatedDate { get; set; }

    [Column("file_extension")]
    [Required]
    [MinLength(3)]
    [MaxLength(10)]
    public string FileExtension { get; set; }

    [Column("checksum")]
    [Required]
    [MinLength(3)]
    [MaxLength(150)]
    public string Checksum { get; set; }

    [Column("checksum_algorithm")]
    [Required]
    [MinLength(3)]
    [MaxLength(10)]
    public string ChecksumAlgorithm { get; set; }

    [NotMapped]
    public Enums.HashingAlgoTypes ChecksumAlgorithmEnum
    {
        get => (Enums.HashingAlgoTypes)Enum.Parse(typeof(Enums.HashingAlgoTypes), ChecksumAlgorithm, true);
        set => ChecksumAlgorithm = value.ToString();
    }

    [Column("number_of_chunks")]
    [Required]
    public int NumberOfChunks { get; set; }

    [Column("create_date")]
    [Required]
    public DateTime CreateDate { get; set; }

    [Column("delete_date")]
    public DateTime? DeleteDate { get; set; }

    [Column("file_process_status")]
    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    public string FileProcessStatus { get; set; }

    [NotMapped]
    public Enums.FileProcessStatus FileProcessStatusEnum
    {
        get => (Enums.FileProcessStatus)Enum.Parse(typeof(Enums.FileProcessStatus), FileProcessStatus, true);
        set => FileProcessStatus = value.ToString();
    }
    
    [Column("last_update_date")]
    public DateTime? LastUpdateDate { get; set; }

    public string GenerateChunkFileName(int index)
    {
        return $"{UniqueIdentifier}.part_{index}";
    }

}
