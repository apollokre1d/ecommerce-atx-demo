using ECommerce.Data.Models;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Order-specific repository interface with business operations
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    // Order processing using stored procedures
    Task<int> ProcessOrderAsync(int customerId, string orderItemsJson, string? shippingAddress = null);
    Task<Order?> GetOrderWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId, int? limit = null);
    
    // Order management
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
    Task<bool> CancelOrderAsync(int orderId, string reason);
    Task<decimal> CalculateOrderTotalAsync(int orderId);
    
    // Order analytics
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
    Task<IEnumerable<Order>> GetHighValueOrdersAsync(decimal minimumAmount, int daysBack = 30);
    Task<decimal> GetTotalSalesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<int> GetOrderCountByStatusAsync(string status);
    
    // Customer order history using stored procedure
    Task<dynamic> GetCustomerOrderHistoryAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, bool includeDetails = false);
    
    // Business intelligence
    Task<IEnumerable<dynamic>> GetSalesTrendsAsync(int daysBack = 30);
    Task<IEnumerable<dynamic>> GetTopCustomersByOrderValueAsync(int count = 10);
}