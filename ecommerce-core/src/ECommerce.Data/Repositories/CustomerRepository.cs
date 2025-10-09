using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Customer repository with business logic and SQL Server optimizations
/// </summary>
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _dbSet
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeCustomerId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var query = _dbSet.Where(c => c.Email.ToLower() == email.ToLower());
        
        if (excludeCustomerId.HasValue)
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Customer>();

        var term = searchTerm.ToLower();
        
        return await _dbSet
            .Where(c => c.IsActive && (
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.Contains(searchTerm))
            ))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<dynamic> GetCustomerOrderSummaryAsync(int customerId, int monthsBack = 12)
    {
        // Use SQL Server table-valued function
        var customerParam = new SqlParameter("@CustomerId", customerId);
        var monthsParam = new SqlParameter("@MonthsBack", monthsBack);

        var result = await _context.Database
            .SqlQueryRaw<dynamic>(@"
                SELECT * FROM dbo.GetCustomerOrderSummary(@CustomerId, @MonthsBack)",
                customerParam, monthsParam)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<string> GetCustomerLoyaltyLevelAsync(int customerId)
    {
        // Use SQL Server scalar function
        var customerParam = new SqlParameter("@CustomerId", customerId);

        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT dbo.GetCustomerLoyaltyLevel(@CustomerId)", customerParam)
            .FirstOrDefaultAsync();

        return result ?? "New";
    }

    public async Task<IEnumerable<Customer>> GetTopCustomersAsync(int count = 10, int monthsBack = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-monthsBack);

        return await _context.Customers
            .Where(c => c.IsActive)
            .Select(c => new
            {
                Customer = c,
                TotalSpent = c.Orders
                    .Where(o => o.OrderDate >= cutoffDate && 
                               o.Status != "Cancelled" && 
                               o.Status != "Refunded")
                    .Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(count)
            .Select(x => x.Customer)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> GetCustomersWithoutOrdersAsync(int daysBack = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

        return await _dbSet
            .Where(c => c.IsActive && 
                       !c.Orders.Any(o => o.OrderDate >= cutoffDate))
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();
    }

    public async Task<int> DeactivateCustomerAsync(int customerId)
    {
        var customerParam = new SqlParameter("@CustomerId", customerId);

        return await _context.Database
            .ExecuteSqlRawAsync(@"
                UPDATE Customers 
                SET IsActive = 0, 
                    ModifiedDate = GETUTCDATE()
                WHERE CustomerId = @CustomerId",
                customerParam);
    }

    public async Task<int> ReactivateCustomerAsync(int customerId)
    {
        var customerParam = new SqlParameter("@CustomerId", customerId);

        return await _context.Database
            .ExecuteSqlRawAsync(@"
                UPDATE Customers 
                SET IsActive = 1, 
                    ModifiedDate = GETUTCDATE()
                WHERE CustomerId = @CustomerId",
                customerParam);
    }

    public Task<bool> ValidateCustomerDataAsync(Customer customer)
    {
        if (customer == null)
            return Task.FromResult(false);

        // Basic validation rules
        var isValid = !string.IsNullOrWhiteSpace(customer.FirstName) &&
                     !string.IsNullOrWhiteSpace(customer.LastName) &&
                     !string.IsNullOrWhiteSpace(customer.Email) &&
                     IsValidEmail(customer.Email) &&
                     customer.FirstName.Length >= 2 &&
                     customer.LastName.Length >= 2;

        return Task.FromResult(isValid);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    // Override to include orders by default
    public override async Task<Customer?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Orders.Where(o => o.Status != "Cancelled"))
            .FirstOrDefaultAsync(c => c.CustomerId == id);
    }
}