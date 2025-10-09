namespace ECommerce.API.DTOs;

/// <summary>
/// Customer data transfer object
/// </summary>
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string LoyaltyLevel { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

/// <summary>
/// Customer creation request
/// </summary>
public class CreateCustomerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

/// <summary>
/// Customer update request
/// </summary>
public class UpdateCustomerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Customer search request
/// </summary>
public class CustomerSearchDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? LoyaltyLevel { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Customer order summary
/// </summary>
public class CustomerOrderSummaryDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public int CustomerLifespanDays { get; set; }
    public int DaysSinceLastOrder { get; set; }
    public string LoyaltyLevel { get; set; } = string.Empty;
    public string FormattedTotalSpent { get; set; } = string.Empty;
}