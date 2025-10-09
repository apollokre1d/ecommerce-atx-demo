using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AuditLogId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TableName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Action { get; set; } = string.Empty;

    public int? RecordId { get; set; }

    [Required]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; }
}