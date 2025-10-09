using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Order repository with complex business logic and SQL Server stored procedures
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<List<Order>> GetOrdersAsync(int? customerId = null, string? status = null, int? limit = null)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<int> ProcessOrderAsync(int customerId, string orderItemsJson, string? shippingAddress = null)
    {
        // Use SQL Server stored procedure for order processing
        var customerParam = new SqlParameter("@CustomerId", customerId);
        var orderItemsParam = new SqlParameter("@OrderItemsJson", orderItemsJson);
        var shippingParam = new SqlParameter("@ShippingAddress", shippingAddress ?? (object)DBNull.Value);
        var orderIdParam = new SqlParameter("@OrderId", System.Data.SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC ProcessOrder @CustomerId, @OrderItemsJson, @ShippingAddress, @OrderId OUTPUT",
            customerParam, orderItemsParam, shippingParam, orderIdParam);

        return (int)orderIdParam.Value;
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId, int? limit = null)
    {
        IQueryable<Order> query = _dbSet
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" };
        if (!validStatuses.Contains(newStatus))
            return false;

        var orderParam = new SqlParameter("@OrderId", orderId);
        var statusParam = new SqlParameter("@NewStatus", newStatus);

        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE Orders 
            SET Status = @NewStatus, ModifiedDate = GETUTCDATE()
            WHERE OrderId = @OrderId",
            orderParam, statusParam);

        return rowsAffected > 0;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason)
    {
        // Check if order can be cancelled
        var order = await GetByIdAsync(orderId);
        if (order == null || order.Status == "Delivered" || order.Status == "Cancelled")
            return false;

        var orderParam = new SqlParameter("@OrderId", orderId);
        var reasonParam = new SqlParameter("@Reason", reason);

        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE Orders 
            SET Status = 'Cancelled', ModifiedDate = GETUTCDATE()
            WHERE OrderId = @OrderId AND Status NOT IN ('Delivered', 'Cancelled');
            
            INSERT INTO AuditLog (TableName, Action, RecordId, UserId, Timestamp, NewValues)
            VALUES ('Orders', 'CANCELLED', @OrderId, SUSER_SNAME(), GETUTCDATE(), @Reason);",
            orderParam, reasonParam);

        return rowsAffected > 0;
    }

    public async Task<decimal> CalculateOrderTotalAsync(int orderId)
    {
        // Use SQL Server function
        var orderParam = new SqlParameter("@OrderId", orderId);
        var subTotal = await _context.Database
            .SqlQueryRaw<decimal>(@"SELECT ISNULL(SUM(LineTotal), 0) FROM OrderItems WHERE OrderId = @OrderId", orderParam)
            .FirstOrDefaultAsync();

        // Use the CalculateOrderTotal function
        var totalParam = new SqlParameter("@SubTotal", subTotal);
        var total = await _context.Database
            .SqlQueryRaw<decimal>("SELECT dbo.CalculateOrderTotal(@SubTotal, DEFAULT, DEFAULT)", totalParam)
            .FirstOrDefaultAsync();

        return total;
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
    {
        return await _dbSet
            .Where(o => o.Status == status)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetHighValueOrdersAsync(decimal minimumAmount, int daysBack = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
        return await _dbSet
            .Where(o => o.TotalAmount >= minimumAmount && o.OrderDate >= cutoffDate)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.TotalAmount)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        IQueryable<Order> query = _dbSet.Where(o => o.Status == "Delivered");

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        return await query.SumAsync(o => o.TotalAmount);
    }

    public async Task<int> GetOrderCountByStatusAsync(string status)
    {
        return await _dbSet.CountAsync(o => o.Status == status);
    }

    public async Task<dynamic> GetCustomerOrderHistoryAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null, bool includeDetails = false)
    {
        // Use SQL Server stored procedure
        var customerParam = new SqlParameter("@CustomerId", customerId);
        var startParam = new SqlParameter("@StartDate", startDate ?? (object)DBNull.Value);
        var endParam = new SqlParameter("@EndDate", endDate ?? (object)DBNull.Value);
        var detailsParam = new SqlParameter("@IncludeDetails", includeDetails);

        var result = await _context.Database.SqlQueryRaw<dynamic>(@"
            EXEC GetCustomerOrderHistory @CustomerId, @StartDate, @EndDate, @IncludeDetails",
            customerParam, startParam, endParam, detailsParam).ToListAsync();

        return result;
    }

    public async Task<IEnumerable<dynamic>> GetSalesTrendsAsync(int daysBack = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
        var trends = await _context.Database.SqlQueryRaw<dynamic>(@"
            SELECT 
                CAST(OrderDate AS DATE) as OrderDate,
                COUNT(*) as OrderCount,
                SUM(TotalAmount) as TotalSales,
                AVG(TotalAmount) as AverageOrderValue
            FROM Orders 
            WHERE OrderDate >= @CutoffDate AND Status = 'Delivered'
            GROUP BY CAST(OrderDate AS DATE)
            ORDER BY CAST(OrderDate AS DATE)",
            new SqlParameter("@CutoffDate", cutoffDate)).ToListAsync();

        return trends;
    }

    public async Task<IEnumerable<dynamic>> GetTopCustomersByOrderValueAsync(int count = 10)
    {
        var topCustomers = await _context.Database.SqlQueryRaw<dynamic>(@"
            SELECT TOP (@Count)
                c.CustomerId,
                c.FirstName + ' ' + c.LastName as CustomerName,
                c.Email,
                COUNT(o.OrderId) as OrderCount,
                SUM(o.TotalAmount) as TotalSpent,
                AVG(o.TotalAmount) as AverageOrderValue,
                MAX(o.OrderDate) as LastOrderDate
            FROM Customers c
            INNER JOIN Orders o ON c.CustomerId = o.CustomerId
            WHERE o.Status = 'Delivered'
            GROUP BY c.CustomerId, c.FirstName, c.LastName, c.Email
            ORDER BY SUM(o.TotalAmount) DESC",
            new SqlParameter("@Count", count)).ToListAsync();

        return topCustomers;
    }

    // Override to include related data by default
    public override async Task<Order?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }
}
