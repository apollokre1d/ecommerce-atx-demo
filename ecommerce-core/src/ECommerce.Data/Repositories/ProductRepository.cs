using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Product repository with SQL Server specific implementations
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsByCategoryAsync(
        int categoryId, 
        int pageNumber = 1, 
        int pageSize = 10, 
        string sortBy = "Name", 
        string sortDirection = "ASC")
    {
        // Use SQL Server stored procedure
        var categoryParam = new SqlParameter("@CategoryId", categoryId);
        var pageNumberParam = new SqlParameter("@PageNumber", pageNumber);
        var pageSizeParam = new SqlParameter("@PageSize", pageSize);
        var sortByParam = new SqlParameter("@SortBy", sortBy);
        var sortDirectionParam = new SqlParameter("@SortDirection", sortDirection);

        // Execute stored procedure for products
        var products = await _context.Products
            .FromSqlRaw("EXEC GetProductsByCategory @CategoryId, @PageNumber, @PageSize, @SortBy, @SortDirection",
                       categoryParam, pageNumberParam, pageSizeParam, sortByParam, sortDirectionParam)
            .Include(p => p.Category)
            .ToListAsync();

        // Get total count (stored procedure returns this in second result set)
        var totalCountParam = new SqlParameter("@CategoryId", categoryId);
        var totalCount = await _context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) FROM Products WHERE CategoryId = @CategoryId AND IsActive = 1", totalCountParam)
            .FirstOrDefaultAsync();

        return (products, totalCount);
    }

    public async Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await _dbSet
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice && p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Product>();

        // Use SQL Server full-text search
        var searchParam = new SqlParameter("@SearchTerm", $"\"{searchTerm}*\"");
        
        return await _context.Products
            .FromSqlRaw(@"
                SELECT p.* FROM Products p
                WHERE CONTAINS((Name, Description), @SearchTerm)
                AND p.IsActive = 1
                ORDER BY p.Name", searchParam)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetTopSellingProductsAsync(int count = 10, int daysBack = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

        return await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                Product = p,
                SalesCount = p.OrderItems
                    .Where(oi => oi.Order.OrderDate >= cutoffDate && 
                                oi.Order.Status == "Delivered")
                    .Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.SalesCount)
            .Take(count)
            .Select(x => x.Product)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsWithLowStockAsync(int threshold = 10)
    {
        // This would typically check against an inventory table
        // For demo purposes, we'll use a placeholder query
        return await _dbSet
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Take(5) // Placeholder - would be actual stock check
            .ToListAsync();
    }

    public async Task<decimal> GetAverageProductPriceAsync(int? categoryId = null)
    {
        var query = _dbSet.Where(p => p.IsActive);
        
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await query.AverageAsync(p => p.Price);
    }

    public async Task<int> GetProductCountByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .CountAsync(p => p.CategoryId == categoryId && p.IsActive);
    }

    public async Task<IEnumerable<dynamic>> GetProductSalesAnalysisAsync(int? categoryId = null, int daysBack = 90)
    {
        // Use SQL Server table-valued function
        var categoryParam = new SqlParameter("@CategoryId", categoryId ?? (object)DBNull.Value);
        var daysBackParam = new SqlParameter("@DaysBack", daysBack);

        var results = await _context.Database
            .SqlQueryRaw<dynamic>(@"
                SELECT * FROM dbo.GetProductSalesAnalysis(@CategoryId, @DaysBack)
                ORDER BY TotalRevenue DESC",
                categoryParam, daysBackParam)
            .ToListAsync();

        return results;
    }

    public async Task<int> BulkUpdatePricesAsync(int categoryId, decimal priceMultiplier)
    {
        // Use SQL Server bulk update
        var categoryParam = new SqlParameter("@CategoryId", categoryId);
        var multiplierParam = new SqlParameter("@PriceMultiplier", priceMultiplier);

        return await _context.Database
            .ExecuteSqlRawAsync(@"
                UPDATE Products 
                SET Price = Price * @PriceMultiplier,
                    ModifiedDate = GETUTCDATE()
                WHERE CategoryId = @CategoryId AND IsActive = 1",
                categoryParam, multiplierParam);
    }

    public async Task<int> BulkDeactivateProductsAsync(IEnumerable<int> productIds)
    {
        if (!productIds.Any())
            return 0;

        var ids = string.Join(",", productIds);
        
        return await _context.Database
            .ExecuteSqlRawAsync($@"
                UPDATE Products 
                SET IsActive = 0,
                    ModifiedDate = GETUTCDATE()
                WHERE ProductId IN ({ids})");
    }

    // Override to include related data by default
    public override async Task<Product?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}