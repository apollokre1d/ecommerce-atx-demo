using AutoMapper;
using ECommerce.API.DTOs;
using ECommerce.Data.Models;
using ECommerce.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ECommerce.API.Controllers;

/// <summary>
/// Orders API controller with stored procedure integration and business logic
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrdersController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with optional filtering and pagination
    /// </summary>
    /// <param name="search">Search parameters</param>
    /// <returns>Paginated list of orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] OrderSearchDto search)
    {
        try
        {
            _logger.LogInformation("Getting orders with search parameters: {@Search}", search);

            IEnumerable<Order> orders;

            if (search.CustomerId.HasValue)
            {
                orders = await _unitOfWork.Orders.GetCustomerOrdersAsync(search.CustomerId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(search.Status))
            {
                orders = await _unitOfWork.Orders.GetOrdersByStatusAsync(search.Status);
            }
            else if (search.StartDate.HasValue || search.EndDate.HasValue)
            {
                var startDate = search.StartDate ?? DateTime.UtcNow.AddMonths(-1);
                var endDate = search.EndDate ?? DateTime.UtcNow;
                orders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(startDate, endDate);
            }
            else if (search.MinAmount.HasValue)
            {
                var minAmount = search.MinAmount.Value;
                orders = await _unitOfWork.Orders.GetHighValueOrdersAsync(minAmount);
            }
            else
            {
                var result = await _unitOfWork.Orders.GetPagedAsync(
                    search.PageNumber,
                    search.PageSize,
                    orderBy: q => q.OrderByDescending(o => o.OrderDate));
                
                orders = result.Items;
            }

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return Ok(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    /// <summary>
    /// Get a specific order by ID with full details
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details with items</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
            
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            var orderDto = _mapper.Map<OrderDto>(order);
            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            return StatusCode(500, "An error occurred while retrieving the order");
        }
    }

    /// <summary>
    /// Create a new order using stored procedure
    /// </summary>
    /// <param name="createOrderDto">Order creation data</param>
    /// <returns>Order processing result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderProcessingResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderProcessingResultDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            // Validate customer exists
            var customer = await _unitOfWork.Customers.GetByIdAsync(createOrderDto.CustomerId);
            if (customer == null)
            {
                return BadRequest($"Customer with ID {createOrderDto.CustomerId} not found");
            }

            if (!customer.IsActive)
            {
                return BadRequest($"Customer with ID {createOrderDto.CustomerId} is not active");
            }

            // Validate order items
            if (!createOrderDto.OrderItems.Any())
            {
                return BadRequest("Order must contain at least one item");
            }

            // Validate all products exist and are active
            foreach (var item in createOrderDto.OrderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    return BadRequest($"Product with ID {item.ProductId} not found or inactive");
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest($"Quantity must be greater than 0 for product {item.ProductId}");
                }

                if (item.UnitPrice <= 0)
                {
                    return BadRequest($"Unit price must be greater than 0 for product {item.ProductId}");
                }
            }

            // Convert order items to JSON for stored procedure
            var orderItemsJson = JsonSerializer.Serialize(createOrderDto.OrderItems.Select(item => new
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }));

            // Use stored procedure to process the order
            var orderId = await _unitOfWork.Orders.ProcessOrderAsync(
                createOrderDto.CustomerId,
                orderItemsJson,
                createOrderDto.ShippingAddress);

            // Get the created order details
            var createdOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(orderId);
            
            if (createdOrder == null)
            {
                return StatusCode(500, "Order was created but could not be retrieved");
            }

            var result = new OrderProcessingResultDto
            {
                OrderId = orderId,
                SubTotal = createdOrder.TotalAmount - createdOrder.TaxAmount,
                TaxAmount = createdOrder.TaxAmount,
                TotalAmount = createdOrder.TotalAmount,
                OrderDate = createdOrder.OrderDate,
                Message = "Order processed successfully",
                Success = true
            };

            _logger.LogInformation("Created order {OrderId} for customer {CustomerId} with total {TotalAmount}", 
                orderId, createOrderDto.CustomerId, createdOrder.TotalAmount);

            return CreatedAtAction(nameof(GetOrder), new { id = orderId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer {CustomerId}", createOrderDto.CustomerId);
            return StatusCode(500, "An error occurred while processing the order");
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="updateStatusDto">Status update data</param>
    /// <returns>Updated order</returns>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateStatusDto)
    {
        try
        {
            var success = await _unitOfWork.Orders.UpdateOrderStatusAsync(id, updateStatusDto.Status);
            
            if (!success)
            {
                return NotFound($"Order with ID {id} not found or status update failed");
            }

            var updatedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
            var orderDto = _mapper.Map<OrderDto>(updatedOrder);

            _logger.LogInformation("Updated order {OrderId} status to {Status}", id, updateStatusDto.Status);

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for order {OrderId}", id);
            return StatusCode(500, "An error occurred while updating the order status");
        }
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>No content</returns>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] string reason = "Cancelled by user")
    {
        try
        {
            var success = await _unitOfWork.Orders.CancelOrderAsync(id, reason);
            
            if (!success)
            {
                return BadRequest($"Order with ID {id} cannot be cancelled (not found or already delivered/cancelled)");
            }

            _logger.LogInformation("Cancelled order {OrderId} with reason: {Reason}", id, reason);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(500, "An error occurred while cancelling the order");
        }
    }

    /// <summary>
    /// Get customer order history using stored procedure
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="includeDetails">Include order item details</param>
    /// <returns>Customer order history</returns>
    [HttpGet("customer/{customerId:int}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<dynamic>> GetCustomerOrderHistory(
        int customerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeDetails = false)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            if (customer == null)
            {
                return NotFound($"Customer with ID {customerId} not found");
            }

            var history = await _unitOfWork.Orders.GetCustomerOrderHistoryAsync(
                customerId, startDate, endDate, includeDetails);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order history for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving order history");
        }
    }

    /// <summary>
    /// Get sales trends analysis
    /// </summary>
    /// <param name="daysBack">Number of days to analyze</param>
    /// <returns>Sales trends data</returns>
    [HttpGet("analytics/sales-trends")]
    [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<dynamic>>> GetSalesTrends([FromQuery] int daysBack = 30)
    {
        try
        {
            var trends = await _unitOfWork.Orders.GetSalesTrendsAsync(daysBack);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales trends");
            return StatusCode(500, "An error occurred while retrieving sales trends");
        }
    }

    /// <summary>
    /// Get total sales for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Total sales amount</returns>
    [HttpGet("analytics/total-sales")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> GetTotalSales(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var totalSales = await _unitOfWork.Orders.GetTotalSalesAsync(startDate, endDate);
            return Ok(totalSales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total sales");
            return StatusCode(500, "An error occurred while retrieving total sales");
        }
    }

    /// <summary>
    /// Get order count by status
    /// </summary>
    /// <param name="status">Order status</param>
    /// <returns>Order count</returns>
    [HttpGet("analytics/count-by-status")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetOrderCountByStatus([FromQuery] string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest("Status parameter is required");
            }

            var count = await _unitOfWork.Orders.GetOrderCountByStatusAsync(status);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order count for status {Status}", status);
            return StatusCode(500, "An error occurred while retrieving order count");
        }
    }

    /// <summary>
    /// Calculate order total using SQL Server function
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Calculated order total</returns>
    [HttpGet("{id:int}/calculate-total")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<decimal>> CalculateOrderTotal(int id)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            var calculatedTotal = await _unitOfWork.Orders.CalculateOrderTotalAsync(id);
            return Ok(calculatedTotal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total for order {OrderId}", id);
            return StatusCode(500, "An error occurred while calculating order total");
        }
    }
}