using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ECommerce.Data.Models;

namespace ECommerce.Data;

public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) 
        : base(options) 
    { 
    }

    // DbSets for all entities
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration with SQL Server specifics
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            
            // SQL Server Identity column
            entity.Property(e => e.ProductId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.Name)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Price)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            // SQL Server default value
            entity.Property(e => e.CreatedDate)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.ModifiedDate)
                  .HasColumnType("datetime2");

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            // SQL Server computed column
            entity.Property(e => e.SearchVector)
                  .HasComputedColumnSql("([Name] + ' ' + ISNULL([Description],''))", stored: true);

            // Foreign key relationship
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index for performance
            entity.HasIndex(e => e.CategoryId)
                  .HasDatabaseName("IX_Products_CategoryId");

            entity.HasIndex(e => e.IsActive)
                  .HasDatabaseName("IX_Products_IsActive");
        });

        // Category configuration with self-referencing relationship
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            
            entity.Property(e => e.CategoryId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.DisplayOrder)
                  .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            // Self-referencing relationship
            entity.HasOne(e => e.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index for hierarchical queries
            entity.HasIndex(e => e.ParentCategoryId)
                  .HasDatabaseName("IX_Categories_ParentCategoryId");
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            
            entity.Property(e => e.CustomerId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.FirstName)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.LastName)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Email)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.Phone)
                  .HasMaxLength(20);

            entity.Property(e => e.CreatedDate)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            // Unique constraint on email
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Customers_Email_Unique");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            
            entity.Property(e => e.OrderId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.OrderDate)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.TotalAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(e => e.TaxAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(e => e.Status)
                  .HasMaxLength(20)
                  .HasDefaultValue("Pending");

            entity.Property(e => e.ShippingAddress)
                  .HasMaxLength(500);

            // Foreign key relationship
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes for common queries
            entity.HasIndex(e => e.CustomerId)
                  .HasDatabaseName("IX_Orders_CustomerId");

            entity.HasIndex(e => e.OrderDate)
                  .HasDatabaseName("IX_Orders_OrderDate");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_Orders_Status");
        });

        // OrderItem configuration with computed column
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);
            
            entity.Property(e => e.OrderItemId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.Quantity)
                  .IsRequired();

            entity.Property(e => e.UnitPrice)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            // SQL Server computed column
            entity.Property(e => e.LineTotal)
                  .HasColumnType("decimal(18,2)")
                  .HasComputedColumnSql("([Quantity] * [UnitPrice])", stored: true);

            // Foreign key relationships
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Composite index for performance
            entity.HasIndex(e => new { e.OrderId, e.ProductId })
                  .HasDatabaseName("IX_OrderItems_OrderId_ProductId");
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            
            entity.Property(e => e.AuditLogId)
                  .UseIdentityColumn(1, 1);

            entity.Property(e => e.TableName)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Action)
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.UserId)
                  .HasMaxLength(128)
                  .IsRequired();

            entity.Property(e => e.Timestamp)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.OldValues)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.NewValues)
                  .HasColumnType("nvarchar(max)");

            // Index for audit queries
            entity.HasIndex(e => new { e.TableName, e.Timestamp })
                  .HasDatabaseName("IX_AuditLog_TableName_Timestamp");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by dependency injection
            optionsBuilder.UseSqlServer("Server=localhost;Database=ECommerceDB;Trusted_Connection=true;TrustServerCertificate=true;");
        }

        // SQL Server specific configurations
        optionsBuilder.UseSqlServer(options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            
            options.CommandTimeout(30);
        });

        // Enable sensitive data logging in development
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }

    // Method to execute stored procedures
    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId, int pageSize, int pageNumber)
    {
        var categoryParam = new SqlParameter("@CategoryId", categoryId);
        var pageSizeParam = new SqlParameter("@PageSize", pageSize);
        var pageNumberParam = new SqlParameter("@PageNumber", pageNumber);

        return await Products
            .FromSqlRaw("EXEC GetProductsByCategory @CategoryId, @PageSize, @PageNumber", 
                       categoryParam, pageSizeParam, pageNumberParam)
            .ToListAsync();
    }

    // Method to process orders via stored procedure
    public async Task<int> ProcessOrderAsync(int customerId, string orderItemsJson, decimal taxRate = 0.0825m)
    {
        var customerParam = new SqlParameter("@CustomerId", customerId);
        var orderItemsParam = new SqlParameter("@OrderItems", orderItemsJson);
        var taxRateParam = new SqlParameter("@TaxRate", taxRate);

        var result = await Database
            .SqlQueryRaw<int>("EXEC ProcessOrder @CustomerId, @OrderItems, @TaxRate", 
                             customerParam, orderItemsParam, taxRateParam)
            .FirstOrDefaultAsync();

        return result;
    }
}