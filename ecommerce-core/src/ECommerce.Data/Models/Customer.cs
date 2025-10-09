using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Computed property for display
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}