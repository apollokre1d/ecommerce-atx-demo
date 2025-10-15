using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("products", Schema = "ecommercedb_dbo")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Required]
    [Column("price", TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column("categoryid")]
    public int CategoryId { get; set; }

    [Column("createddate", TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifieddate", TypeName = "datetime2")]
    public DateTime? ModifiedDate { get; set; }

    [Column("isactive")]
    public bool IsActive { get; set; } = true;

    // Computed columns - will be configured in DbContext
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column("searchvector")]
    public string? SearchVector { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column("pricecategory")]
    public string? PriceCategory { get; set; }

    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}