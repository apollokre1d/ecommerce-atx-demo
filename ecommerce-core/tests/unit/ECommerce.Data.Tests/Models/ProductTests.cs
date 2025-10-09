using ECommerce.Data.Models;
using FluentAssertions;
using Xunit;

namespace ECommerce.Data.Tests.Models;

public class ProductTests
{
    [Fact]
    public void Product_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Name.Should().Be(string.Empty);
        product.IsActive.Should().BeTrue();
        product.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        product.OrderItems.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Product_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Electronics" };
        var testDate = DateTime.UtcNow;

        // Act
        var product = new Product
        {
            ProductId = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            CategoryId = 1,
            Category = category,
            CreatedDate = testDate,
            IsActive = true
        };

        // Assert
        product.ProductId.Should().Be(1);
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Test Description");
        product.Price.Should().Be(99.99m);
        product.CategoryId.Should().Be(1);
        product.Category.Should().Be(category);
        product.CreatedDate.Should().Be(testDate);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Product_NavigationProperties_ShouldBeInitialized()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.OrderItems.Should().NotBeNull();
        product.OrderItems.Should().BeOfType<List<OrderItem>>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public void Product_WithInvalidPrice_ShouldAllowNegativeValues(decimal price)
    {
        // Note: Business validation should be handled at service layer
        // Entity models allow any decimal value
        
        // Arrange & Act
        var product = new Product { Price = price };

        // Assert
        product.Price.Should().Be(price);
    }
}