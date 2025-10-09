using ECommerce.Data.Models;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Product-specific repository interface with SQL Server stored procedure support
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    // SQL Server stored procedure methods
    Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsByCategoryAsync(
        int categoryId, 
        int pageNumber = 1, 
        int pageSize = 10, 
        string sortBy = "Name", 
        string sortDirection = "ASC");

    // Advanced product queries
    Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    Task<IEnumerable<Product>> GetTopSellingProductsAsync(int count = 10, int daysBack = 30);
    Task<IEnumerable<Product>> GetProductsWithLowStockAsync(int threshold = 10);
    
    // Product analytics using SQL Server functions
    Task<decimal> GetAverageProductPriceAsync(int? categoryId = null);
    Task<int> GetProductCountByCategoryAsync(int categoryId);
    Task<IEnumerable<dynamic>> GetProductSalesAnalysisAsync(int? categoryId = null, int daysBack = 90);
    
    // Bulk operations
    Task<int> BulkUpdatePricesAsync(int categoryId, decimal priceMultiplier);
    Task<int> BulkDeactivateProductsAsync(IEnumerable<int> productIds);
}