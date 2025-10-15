using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("customers", Schema = "ecommercedb_dbo")]
public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("customerid")]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("firstname")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("lastname")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Column("createddate", TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifieddate", TypeName = "datetime2")]
    public DateTime? ModifiedDate { get; set; }

    [Column("isactive")]
    public bool IsActive { get; set; } = true;

    // Computed property for display
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}