using ECommerce.Data;
using ECommerce.Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Data.Tests;

public class DbContextTests : IDisposable
{
    private readonly ECommerceDbContext _context;

    public DbContextTests()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ECommerceDbContext(options);
    }

    [Fact]
    public void DbContext_ShouldHaveAllDbSetsConfigured()
    {
        // Assert
        _context.Products.Should().NotBeNull();
        _context.Categories.Should().NotBeNull();
        _context.Customers.Should().NotBeNull();
        _context.Orders.Should().NotBeNull();
        _context.OrderItems.Should().NotBeNull();
        _context.AuditLogs.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics",
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var retrievedCategory = await _context.Categories.FirstOrDefaultAsync();

        // Assert
        retrievedCategory.Should().NotBeNull();
        retrievedCategory!.Name.Should().Be("Electronics");
        retrievedCategory.DisplayOrder.Should().Be(1);
        retrievedCategory.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DbContext_CanAddProductWithCategory()
    {
        // Arrange
        var category = new Category { Name = "Electronics" };
        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 1299.99m,
            Category = category
        };

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var retrievedProduct = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync();

        // Assert
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Name.Should().Be("Laptop");
        retrievedProduct.Price.Should().Be(1299.99m);
        retrievedProduct.Category.Should().NotBeNull();
        retrievedProduct.Category.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task DbContext_CanCreateOrderWithItems()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        var category = new Category { Name = "Electronics" };
        var product = new Product
        {
            Name = "Mouse",
            Price = 29.99m,
            Category = category
        };

        var order = new Order
        {
            Customer = customer,
            TotalAmount = 32.49m,
            TaxAmount = 2.50m,
            Status = "Processing"
        };

        var orderItem = new OrderItem
        {
            Order = order,
            Product = product,
            Quantity = 1,
            UnitPrice = 29.99m
        };

        // Act
        _context.Orders.Add(order);
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        var retrievedOrder = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync();

        // Assert
        retrievedOrder.Should().NotBeNull();
        retrievedOrder!.Customer.FullName.Should().Be("John Doe");
        retrievedOrder.TotalAmount.Should().Be(32.49m);
        retrievedOrder.OrderItems.Should().HaveCount(1);
        retrievedOrder.OrderItems.First().Product.Name.Should().Be("Mouse");
    }

    [Fact]
    public async Task DbContext_CanAddAuditLog()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            TableName = "Products",
            Action = "INSERT",
            RecordId = 1,
            UserId = "TestUser",
            OldValues = null,
            NewValues = "{\"Name\":\"Test Product\",\"Price\":99.99}"
        };

        // Act
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var retrievedLog = await _context.AuditLogs.FirstOrDefaultAsync();

        // Assert
        retrievedLog.Should().NotBeNull();
        retrievedLog!.TableName.Should().Be("Products");
        retrievedLog.Action.Should().Be("INSERT");
        retrievedLog.UserId.Should().Be("TestUser");
        retrievedLog.NewValues.Should().Contain("Test Product");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}