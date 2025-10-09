using AutoMapper;
using ECommerce.API.DTOs;
using ECommerce.Data.Models;
using ECommerce.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Products API controller with advanced SQL Server features
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductsController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with optional filtering and pagination
    /// </summary>
    /// <param name="search">Search parameters</param>
    /// <returns>Paginated list of products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedProductsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedProductsDto>> GetProducts([FromQuery] ProductSearchDto search)
    {
        try
        {
            _logger.LogInformation("Getting products with search parameters: {@Search}", search);

            IEnumerable<Product> products;
            int totalCount;

            // Use different repository methods based on search criteria
            if (!string.IsNullOrWhiteSpace(search.SearchTerm))
            {
                // Use full-text search
                products = await _unitOfWork.Products.SearchProductsAsync(search.SearchTerm);
                totalCount = products.Count();
                
                // Apply pagination manually for search results
                products = products
                    .Skip((search.PageNumber - 1) * search.PageSize)
                    .Take(search.PageSize);
            }
            else if (search.CategoryId.HasValue)
            {
                // Use stored procedure for category-based search with pagination
                var result = await _unitOfWork.Products.GetProductsByCategoryAsync(
                    search.CategoryId.Value, 
                    search.PageNumber, 
                    search.PageSize, 
                    search.SortBy, 
                    search.SortDirection);
                
                products = result.Products;
                totalCount = result.TotalCount;
            }
            else if (search.MinPrice.HasValue || search.MaxPrice.HasValue)
            {
                // Use price range search
                var minPrice = search.MinPrice ?? 0;
                var maxPrice = search.MaxPrice ?? decimal.MaxValue;
                
                products = await _unitOfWork.Products.GetProductsByPriceRangeAsync(minPrice, maxPrice);
                totalCount = products.Count();
                
                // Apply pagination manually
                products = products
                    .Skip((search.PageNumber - 1) * search.PageSize)
                    .Take(search.PageSize);
            }
            else
            {
                // Use generic repository with pagination
                var result = await _unitOfWork.Products.GetPagedAsync(
                    search.PageNumber,
                    search.PageSize,
                    filter: p => p.IsActive,
                    orderBy: q => search.SortDirection.ToUpper() == "DESC" 
                        ? q.OrderByDescending(p => p.Name)
                        : q.OrderBy(p => p.Name));
                
                products = result.Items;
                totalCount = result.TotalCount;
            }

            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

            var response = new PagedProductsDto
            {
                Products = productDtos,
                TotalCount = totalCount,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, "An error occurred while retrieving products");
        }
    }

    /// <summary>
    /// Get a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            
            if (product == null)
            {
                return NotFound($"Product with ID {id} not found");
            }

            var productDto = _mapper.Map<ProductDto>(product);
            return Ok(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return StatusCode(500, "An error occurred while retrieving the product");
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        try
        {
            // Validate category exists
            var categoryExists = await _unitOfWork.Categories.ExistsAsync(c => c.CategoryId == createProductDto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest($"Category with ID {createProductDto.CategoryId} does not exist");
            }

            var product = _mapper.Map<Product>(createProductDto);
            
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Reload with category information
            var createdProduct = await _unitOfWork.Products.GetByIdAsync(product.ProductId);
            var productDto = _mapper.Map<ProductDto>(createdProduct);

            _logger.LogInformation("Created product {ProductId}: {ProductName}", product.ProductId, product.Name);

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "An error occurred while creating the product");
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
    {
        try
        {
            var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound($"Product with ID {id} not found");
            }

            // Validate category exists
            var categoryExists = await _unitOfWork.Categories.ExistsAsync(c => c.CategoryId == updateProductDto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest($"Category with ID {updateProductDto.CategoryId} does not exist");
            }

            _mapper.Map(updateProductDto, existingProduct);
            
            await _unitOfWork.Products.UpdateAsync(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            // Reload with category information
            var updatedProduct = await _unitOfWork.Products.GetByIdAsync(id);
            var productDto = _mapper.Map<ProductDto>(updatedProduct);

            _logger.LogInformation("Updated product {ProductId}: {ProductName}", id, existingProduct.Name);

            return Ok(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, "An error occurred while updating the product");
        }
    }

    /// <summary>
    /// Delete a product (soft delete by setting IsActive = false)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound($"Product with ID {id} not found");
            }

            // Soft delete by setting IsActive = false
            product.IsActive = false;
            product.ModifiedDate = DateTime.UtcNow;
            
            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted product {ProductId}: {ProductName}", id, product.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, "An error occurred while deleting the product");
        }
    }

    /// <summary>
    /// Get top selling products
    /// </summary>
    /// <param name="count">Number of products to return</param>
    /// <param name="daysBack">Number of days to look back</param>
    /// <returns>List of top selling products</returns>
    [HttpGet("top-selling")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetTopSellingProducts(
        [FromQuery] int count = 10, 
        [FromQuery] int daysBack = 30)
    {
        try
        {
            var products = await _unitOfWork.Products.GetTopSellingProductsAsync(count, daysBack);
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            
            return Ok(productDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top selling products");
            return StatusCode(500, "An error occurred while retrieving top selling products");
        }
    }

    /// <summary>
    /// Get product sales analysis using SQL Server function
    /// </summary>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="daysBack">Number of days to analyze</param>
    /// <returns>Product sales analysis</returns>
    [HttpGet("sales-analysis")]
    [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<dynamic>>> GetProductSalesAnalysis(
        [FromQuery] int? categoryId = null,
        [FromQuery] int daysBack = 90)
    {
        try
        {
            var analysis = await _unitOfWork.Products.GetProductSalesAnalysisAsync(categoryId, daysBack);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product sales analysis");
            return StatusCode(500, "An error occurred while retrieving sales analysis");
        }
    }

    /// <summary>
    /// Bulk update product prices for a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="priceMultiplier">Price multiplier (e.g., 1.1 for 10% increase)</param>
    /// <returns>Number of products updated</returns>
    [HttpPatch("bulk-update-prices")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> BulkUpdatePrices(
        [FromQuery] int categoryId,
        [FromQuery] decimal priceMultiplier)
    {
        try
        {
            if (priceMultiplier <= 0)
            {
                return BadRequest("Price multiplier must be greater than 0");
            }

            var updatedCount = await _unitOfWork.Products.BulkUpdatePricesAsync(categoryId, priceMultiplier);
            
            _logger.LogInformation("Bulk updated prices for {Count} products in category {CategoryId}", 
                updatedCount, categoryId);

            return Ok(updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating prices for category {CategoryId}", categoryId);
            return StatusCode(500, "An error occurred while updating prices");
        }
    }
}