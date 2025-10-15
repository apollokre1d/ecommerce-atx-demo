using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("auditlogs", Schema = "ecommercedb_dbo")]
public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("auditlogid")]
    public int AuditLogId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("tablename")]
    public string TableName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("recordid")]
    public int? RecordId { get; set; }

    [Required]
    [MaxLength(128)]
    [Column("userid")]
    public string UserId { get; set; } = string.Empty;

    [Column("timestamp", TypeName = "datetime2")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("oldvalues", TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; }

    [Column("newvalues", TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; }
}