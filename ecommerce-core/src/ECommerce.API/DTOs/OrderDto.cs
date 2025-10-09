namespace ECommerce.API.DTOs;

/// <summary>
/// Order data transfer object
/// </summary>
public class OrderDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
}

/// <summary>
/// Order item data transfer object
/// </summary>
public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Order creation request
/// </summary>
public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public string? ShippingAddress { get; set; }
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

/// <summary>
/// Order item creation request
/// </summary>
public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Order status update request
/// </summary>
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Order search request
/// </summary>
public class OrderSearchDto
{
    public int? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Order processing response
/// </summary>
public class OrderProcessingResultDto
{
    public int OrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}