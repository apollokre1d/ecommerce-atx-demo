using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Data.Models;

[Table("categories", Schema = "ecommercedb_dbo")]
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("categoryid")]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("parentcategoryid")]
    public int? ParentCategoryId { get; set; }

    [Column("displayorder")]
    public int DisplayOrder { get; set; } = 0;

    [Column("createddate", TypeName = "datetime2")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifieddate", TypeName = "datetime2")]
    public DateTime? ModifiedDate { get; set; }

    [Column("isactive")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("ParentCategoryId")]
    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}