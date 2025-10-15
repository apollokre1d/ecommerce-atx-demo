using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("orders", Schema = "ecommercedb_dbo")]
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderid")]
    public int OrderId { get; set; }

    [Required]
    [Column("customerid")]
    public int CustomerId { get; set; }

    [Column("orderdate", TypeName = "datetime2")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column("createddate", TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifieddate", TypeName = "datetime2")]
    public DateTime? ModifiedDate { get; set; }

    [Required]
    [Column("totalamount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column("taxamount", TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    [Column("shippingaddress")]
    public string? ShippingAddress { get; set; }

    // Computed property for subtotal
    [NotMapped]
    public decimal SubTotal => TotalAmount - TaxAmount;

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}