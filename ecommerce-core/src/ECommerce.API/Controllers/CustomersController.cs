using AutoMapper;
using ECommerce.API.DTOs;
using ECommerce.Data.Models;
using ECommerce.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Customers API controller with business logic and analytics
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CustomersController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers with optional filtering and pagination
    /// </summary>
    /// <param name="search">Search parameters</param>
    /// <returns>Paginated list of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] CustomerSearchDto search)
    {
        try
        {
            _logger.LogInformation("Getting customers with search parameters: {@Search}", search);

            IEnumerable<Customer> customers;

            if (!string.IsNullOrWhiteSpace(search.SearchTerm))
            {
                customers = await _unitOfWork.Customers.SearchCustomersAsync(search.SearchTerm);
            }
            else if (search.IsActive.HasValue)
            {
                if (search.IsActive.Value)
                {
                    customers = await _unitOfWork.Customers.GetActiveCustomersAsync();
                }
                else
                {
                    customers = await _unitOfWork.Customers.FindAsync(c => !c.IsActive);
                }
            }
            else
            {
                var result = await _unitOfWork.Customers.GetPagedAsync(
                    search.PageNumber,
                    search.PageSize,
                    orderBy: q => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName));
                
                customers = result.Items;
            }

            var customerDtos = new List<CustomerDto>();
            
            foreach (var customer in customers)
            {
                var customerDto = _mapper.Map<CustomerDto>(customer);
                
                // Get loyalty level using SQL Server function
                customerDto.LoyaltyLevel = await _unitOfWork.Customers.GetCustomerLoyaltyLevelAsync(customer.CustomerId);
                
                customerDtos.Add(customerDto);
            }

            return Ok(customerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return StatusCode(500, "An error occurred while retrieving customers");
        }
    }

    /// <summary>
    /// Get a specific customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            
            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            var customerDto = _mapper.Map<CustomerDto>(customer);
            customerDto.LoyaltyLevel = await _unitOfWork.Customers.GetCustomerLoyaltyLevelAsync(id);

            return Ok(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", id);
            return StatusCode(500, "An error occurred while retrieving the customer");
        }
    }

    /// <summary>
    /// Get customer by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>Customer details</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomerByEmail(string email)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByEmailAsync(email);
            
            if (customer == null)
            {
                return NotFound($"Customer with email {email} not found");
            }

            var customerDto = _mapper.Map<CustomerDto>(customer);
            customerDto.LoyaltyLevel = await _unitOfWork.Customers.GetCustomerLoyaltyLevelAsync(customer.CustomerId);

            return Ok(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by email {Email}", email);
            return StatusCode(500, "An error occurred while retrieving the customer");
        }
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="createCustomerDto">Customer creation data</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto createCustomerDto)
    {
        try
        {
            // Check if email already exists
            var emailExists = await _unitOfWork.Customers.EmailExistsAsync(createCustomerDto.Email);
            if (emailExists)
            {
                return BadRequest($"A customer with email {createCustomerDto.Email} already exists");
            }

            var customer = _mapper.Map<Customer>(createCustomerDto);
            
            // Validate customer data
            var isValid = await _unitOfWork.Customers.ValidateCustomerDataAsync(customer);
            if (!isValid)
            {
                return BadRequest("Invalid customer data provided");
            }

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            var customerDto = _mapper.Map<CustomerDto>(customer);
            customerDto.LoyaltyLevel = "New";

            _logger.LogInformation("Created customer {CustomerId}: {CustomerName}", 
                customer.CustomerId, $"{customer.FirstName} {customer.LastName}");

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, "An error occurred while creating the customer");
        }
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="updateCustomerDto">Customer update data</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto updateCustomerDto)
    {
        try
        {
            var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (existingCustomer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            // Check if email already exists (excluding current customer)
            var emailExists = await _unitOfWork.Customers.EmailExistsAsync(updateCustomerDto.Email, id);
            if (emailExists)
            {
                return BadRequest($"A customer with email {updateCustomerDto.Email} already exists");
            }

            _mapper.Map(updateCustomerDto, existingCustomer);
            
            // Validate customer data
            var isValid = await _unitOfWork.Customers.ValidateCustomerDataAsync(existingCustomer);
            if (!isValid)
            {
                return BadRequest("Invalid customer data provided");
            }

            await _unitOfWork.Customers.UpdateAsync(existingCustomer);
            await _unitOfWork.SaveChangesAsync();

            var customerDto = _mapper.Map<CustomerDto>(existingCustomer);
            customerDto.LoyaltyLevel = await _unitOfWork.Customers.GetCustomerLoyaltyLevelAsync(id);

            _logger.LogInformation("Updated customer {CustomerId}: {CustomerName}", 
                id, $"{existingCustomer.FirstName} {existingCustomer.LastName}");

            return Ok(customerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return StatusCode(500, "An error occurred while updating the customer");
        }
    }

    /// <summary>
    /// Deactivate a customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCustomer(int id)
    {
        try
        {
            var rowsAffected = await _unitOfWork.Customers.DeactivateCustomerAsync(id);
            
            if (rowsAffected == 0)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            _logger.LogInformation("Deactivated customer {CustomerId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer {CustomerId}", id);
            return StatusCode(500, "An error occurred while deactivating the customer");
        }
    }

    /// <summary>
    /// Get customer order summary using SQL Server function
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="monthsBack">Number of months to look back</param>
    /// <returns>Customer order summary</returns>
    [HttpGet("{id:int}/order-summary")]
    [ProducesResponseType(typeof(CustomerOrderSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOrderSummaryDto>> GetCustomerOrderSummary(int id, [FromQuery] int monthsBack = 12)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            var summary = await _unitOfWork.Customers.GetCustomerOrderSummaryAsync(id, monthsBack);
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order summary for customer {CustomerId}", id);
            return StatusCode(500, "An error occurred while retrieving customer order summary");
        }
    }

    /// <summary>
    /// Get top customers by order value
    /// </summary>
    /// <param name="count">Number of customers to return</param>
    /// <param name="monthsBack">Number of months to look back</param>
    /// <returns>List of top customers</returns>
    [HttpGet("top-customers")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetTopCustomers(
        [FromQuery] int count = 10,
        [FromQuery] int monthsBack = 12)
    {
        try
        {
            var customers = await _unitOfWork.Customers.GetTopCustomersAsync(count, monthsBack);
            
            var customerDtos = new List<CustomerDto>();
            foreach (var customer in customers)
            {
                var customerDto = _mapper.Map<CustomerDto>(customer);
                customerDto.LoyaltyLevel = await _unitOfWork.Customers.GetCustomerLoyaltyLevelAsync(customer.CustomerId);
                customerDtos.Add(customerDto);
            }

            return Ok(customerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top customers");
            return StatusCode(500, "An error occurred while retrieving top customers");
        }
    }

    /// <summary>
    /// Get customers without recent orders
    /// </summary>
    /// <param name="daysBack">Number of days to look back</param>
    /// <returns>List of inactive customers</returns>
    [HttpGet("inactive")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetInactiveCustomers([FromQuery] int daysBack = 90)
    {
        try
        {
            var customers = await _unitOfWork.Customers.GetCustomersWithoutOrdersAsync(daysBack);
            var customerDtos = _mapper.Map<IEnumerable<CustomerDto>>(customers);

            return Ok(customerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactive customers");
            return StatusCode(500, "An error occurred while retrieving inactive customers");
        }
    }

    /// <summary>
    /// Reactivate a deactivated customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpPatch("{id:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateCustomer(int id)
    {
        try
        {
            var rowsAffected = await _unitOfWork.Customers.ReactivateCustomerAsync(id);
            
            if (rowsAffected == 0)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            _logger.LogInformation("Reactivated customer {CustomerId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating customer {CustomerId}", id);
            return StatusCode(500, "An error occurred while reactivating the customer");
        }
    }
}