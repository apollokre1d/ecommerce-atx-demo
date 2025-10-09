using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Unit of Work implementation with SQL Server transaction support
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ECommerceDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Repository instances
    private IProductRepository? _products;
    private ICustomerRepository? _customers;
    private IOrderRepository? _orders;
    private IRepository<Category>? _categories;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<AuditLog>? _auditLogs;

    public UnitOfWork(ECommerceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Lazy-loaded repository properties
    public IProductRepository Products => 
        _products ??= new ProductRepository(_context);

    public ICustomerRepository Customers => 
        _customers ??= new CustomerRepository(_context);

    public IOrderRepository Orders => 
        _orders ??= new OrderRepository(_context);

    public IRepository<Category> Categories => 
        _categories ??= new Repository<Category>(_context);

    public IRepository<OrderItem> OrderItems => 
        _orderItems ??= new Repository<OrderItem>(_context);

    public IRepository<AuditLog> AuditLogs => 
        _auditLogs ??= new Repository<AuditLog>(_context);

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log the exception details
            throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction in progress.");

        try
        {
            await _transaction.CommitAsync();
        }
        catch
        {
            await _transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction in progress.");

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
    {
        return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task<IEnumerable<T>> SqlQueryAsync<T>(string sql, params object[] parameters) where T : class
    {
        return await _context.Database.SqlQueryRaw<T>(sql, parameters).ToListAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}