using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("orderitems", Schema = "ecommercedb_dbo")]
public class OrderItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderitemid")]
    public int OrderItemId { get; set; }

    [Required]
    [Column("orderid")]
    public int OrderId { get; set; }

    [Required]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    [Column("quantity")]
    public int Quantity { get; set; }

    [Required]
    [Column("unitprice", TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column("createddate", TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Computed column - will be configured in DbContext
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column("linetotal", TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}