using ECommerce.Data.Models;

namespace ECommerce.Data.Repositories;

/// <summary>
/// Customer-specific repository interface
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    // Customer-specific queries
    Task<Customer?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email, int? excludeCustomerId = null);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync();
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
    
    // Customer analytics using SQL Server functions
    Task<dynamic> GetCustomerOrderSummaryAsync(int customerId, int monthsBack = 12);
    Task<string> GetCustomerLoyaltyLevelAsync(int customerId);
    Task<IEnumerable<Customer>> GetTopCustomersAsync(int count = 10, int monthsBack = 12);
    Task<IEnumerable<Customer>> GetCustomersWithoutOrdersAsync(int daysBack = 90);
    
    // Customer management
    Task<int> DeactivateCustomerAsync(int customerId);
    Task<int> ReactivateCustomerAsync(int customerId);
    Task<bool> ValidateCustomerDataAsync(Customer customer);
}