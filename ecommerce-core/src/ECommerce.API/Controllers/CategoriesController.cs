using AutoMapper;
using ECommerce.API.DTOs;
using ECommerce.API.Mapping;
using ECommerce.Data.Models;
using ECommerce.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Categories API controller for product categorization
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoriesController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories with hierarchical structure
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
            
            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found");
            }

            var categoryDto = _mapper.Map<CategoryDto>(category);
            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving the category");
        }
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="createCategoryDto">Category creation data</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
    {
        try
        {
            // Validate parent category exists if specified
            if (createCategoryDto.ParentCategoryId.HasValue)
            {
                var parentExists = await _unitOfWork.Categories.ExistsAsync(c => c.CategoryId == createCategoryDto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest($"Parent category with ID {createCategoryDto.ParentCategoryId} does not exist");
                }
            }

            var category = _mapper.Map<Category>(createCategoryDto);
            
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = _mapper.Map<CategoryDto>(category);

            _logger.LogInformation("Created category {CategoryId}: {CategoryName}", category.CategoryId, category.Name);

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "An error occurred while creating the category");
        }
    }

    /// <summary>
    /// Get products in a specific category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Products in the category</returns>
    [HttpGet("{id:int}/products")]
    [ProducesResponseType(typeof(PagedProductsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedProductsDto>> GetCategoryProducts(
        int id, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found");
            }

            // Use the stored procedure for category-based product retrieval
            var result = await _unitOfWork.Products.GetProductsByCategoryAsync(id, pageNumber, pageSize);
            
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(result.Products);

            var response = new PagedProductsDto
            {
                Products = productDtos,
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for category {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving category products");
        }
    }

    /// <summary>
    /// Get category statistics
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category statistics</returns>
    [HttpGet("{id:int}/statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetCategoryStatistics(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found");
            }

            var productCount = await _unitOfWork.Products.GetProductCountByCategoryAsync(id);
            var averagePrice = await _unitOfWork.Products.GetAverageProductPriceAsync(id);

            var statistics = new
            {
                CategoryId = id,
                CategoryName = category.Name,
                ProductCount = productCount,
                AveragePrice = averagePrice,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for category {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving category statistics");
        }
    }
}