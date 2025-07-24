
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileChunkerDemo.Models;

[Table("tbl_stored_file")]
public class StoredFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    
    // [Column("unique_identifier")]
    // [Required]
    // [MinLength(3)]
    // [MaxLength(50)]
    // public string UniqueIdentifier { get; set; }
    
    [Column("file_name")]
    [Required]
    [MinLength(3)]
    [MaxLength(150)]
    public string FileName { get; set; }
    
    [Column("content")]
    [Required]
    [MinLength(3)]
    public string Content { get; set; }
    
    [Column("create_date")]
    [Required]
    public DateTime CreateDate { get; set; }

    [Column("delete_date")]
    public DateTime? DeleteDate { get; set; }
}