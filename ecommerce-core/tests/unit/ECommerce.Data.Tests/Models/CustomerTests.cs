using ECommerce.Data.Models;
using FluentAssertions;
using Xunit;

namespace ECommerce.Data.Tests.Models;

public class CustomerTests
{
    [Fact]
    public void Customer_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var customer = new Customer();

        // Assert
        customer.FirstName.Should().Be(string.Empty);
        customer.LastName.Should().Be(string.Empty);
        customer.Email.Should().Be(string.Empty);
        customer.IsActive.Should().BeTrue();
        customer.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        customer.Orders.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Customer_FullName_ShouldConcatenateFirstAndLastName()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = customer.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void Customer_FullName_WithEmptyNames_ShouldHandleGracefully()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "",
            LastName = ""
        };

        // Act
        var fullName = customer.FullName;

        // Assert
        fullName.Should().Be(" ");
    }

    [Fact]
    public void Customer_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var testDate = DateTime.UtcNow;

        // Act
        var customer = new Customer
        {
            CustomerId = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Phone = "555-1234",
            CreatedDate = testDate,
            IsActive = true
        };

        // Assert
        customer.CustomerId.Should().Be(1);
        customer.FirstName.Should().Be("Jane");
        customer.LastName.Should().Be("Smith");
        customer.Email.Should().Be("jane.smith@example.com");
        customer.Phone.Should().Be("555-1234");
        customer.CreatedDate.Should().Be(testDate);
        customer.IsActive.Should().BeTrue();
        customer.FullName.Should().Be("Jane Smith");
    }

    [Fact]
    public void Customer_NavigationProperties_ShouldBeInitialized()
    {
        // Arrange & Act
        var customer = new Customer();

        // Assert
        customer.Orders.Should().NotBeNull();
        customer.Orders.Should().BeOfType<List<Order>>();
    }
}