namespace ECommerce.Data.Repositories;

/// <summary>
/// Unit of Work pattern for coordinating repository operations and transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repository properties
    IProductRepository Products { get; }
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IRepository<Models.Category> Categories { get; }
    IRepository<Models.OrderItem> OrderItems { get; }
    IRepository<Models.AuditLog> AuditLogs { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    // Bulk operations
    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
    Task<IEnumerable<T>> SqlQueryAsync<T>(string sql, params object[] parameters) where T : class;
}