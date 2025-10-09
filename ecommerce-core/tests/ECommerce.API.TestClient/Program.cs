using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ConsoleTables;

namespace ECommerce.API.TestClient;

class Program
{
    private static readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static async Task Main(string[] args)
    {
        // Configure services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        Console.WriteLine("🚀 E-Commerce API Test Client");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        // Get API base URL
        string baseUrl = GetApiBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        try
        {
            // Test API connectivity
            await TestApiConnectivity();

            // Run comprehensive tests
            await RunAllTests();

            Console.WriteLine();
            Console.WriteLine("✅ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Test execution failed");
        }
        finally
        {
            _httpClient.Dispose();
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static string GetApiBaseUrl()
    {
        Console.Write("Enter API base URL (default: http://localhost:5000): ");
        string? input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return "http://localhost:5000";
        }

        return input.TrimEnd('/');
    }

    private static async Task TestApiConnectivity()
    {
        Console.WriteLine("🔍 Testing API connectivity...");
        
        try
        {
            var response = await _httpClient.GetAsync("/api/info");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiInfo = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
                Console.WriteLine($"✅ Connected to API: {_httpClient.BaseAddress}");
                Console.WriteLine($"   Environment: {apiInfo?.GetProperty("environment")}");
                Console.WriteLine();
            }
            else
            {
                throw new Exception($"API returned status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to API at {_httpClient.BaseAddress}: {ex.Message}");
        }
    }

    private static async Task RunAllTests()
    {
        Console.WriteLine("🧪 Running comprehensive API tests...");
        Console.WriteLine();

        // Test Categories
        await TestCategoriesEndpoints();
        
        // Test Products
        await TestProductsEndpoints();
        
        // Test Customers
        await TestCustomersEndpoints();
        
        // Test Orders
        await TestOrdersEndpoints();
        
        // Test Analytics
        await TestAnalyticsEndpoints();
    }

    private static async Task TestCategoriesEndpoints()
    {
        Console.WriteLine("📂 Testing Categories API...");
        
        try
        {
            // Get all categories
            var response = await _httpClient.GetAsync("/api/v1/categories");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<JsonElement[]>(content, _jsonOptions);
            
            Console.WriteLine($"   ✅ GET /categories - Found {categories?.Length} categories");
            
            if (categories?.Length > 0)
            {
                var firstCategory = categories[0];
                var categoryId = firstCategory.GetProperty("categoryId").GetInt32();
                
                // Get specific category
                response = await _httpClient.GetAsync($"/api/v1/categories/{categoryId}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /categories/{categoryId} - Category details retrieved");
                
                // Get category products
                response = await _httpClient.GetAsync($"/api/v1/categories/{categoryId}/products");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /categories/{categoryId}/products - Products retrieved");
                
                // Get category statistics
                response = await _httpClient.GetAsync($"/api/v1/categories/{categoryId}/statistics");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /categories/{categoryId}/statistics - Statistics retrieved");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Categories API test failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private static async Task TestProductsEndpoints()
    {
        Console.WriteLine("📦 Testing Products API...");
        
        try
        {
            // Get all products
            var response = await _httpClient.GetAsync("/api/v1/products?pageSize=5");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var productsResponse = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var products = productsResponse.GetProperty("products").EnumerateArray().ToArray();
            
            Console.WriteLine($"   ✅ GET /products - Found {products.Length} products (page 1)");
            
            if (products.Length > 0)
            {
                var firstProduct = products[0];
                var productId = firstProduct.GetProperty("productId").GetInt32();
                
                // Get specific product
                response = await _httpClient.GetAsync($"/api/v1/products/{productId}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /products/{productId} - Product details retrieved");
                
                // Search products
                response = await _httpClient.GetAsync("/api/v1/products?searchTerm=laptop");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /products?searchTerm=laptop - Search completed");
                
                // Get products by category
                var categoryId = firstProduct.GetProperty("categoryId").GetInt32();
                response = await _httpClient.GetAsync($"/api/v1/products?categoryId={categoryId}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /products?categoryId={categoryId} - Category filter works");
                
                // Get top selling products
                response = await _httpClient.GetAsync("/api/v1/products/top-selling?count=5");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /products/top-selling - Top sellers retrieved");
                
                // Get sales analysis
                response = await _httpClient.GetAsync("/api/v1/products/sales-analysis");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /products/sales-analysis - Analytics retrieved");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Products API test failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private static async Task TestCustomersEndpoints()
    {
        Console.WriteLine("👥 Testing Customers API...");
        
        try
        {
            // Get all customers
            var response = await _httpClient.GetAsync("/api/v1/customers?pageSize=5");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<JsonElement[]>(content, _jsonOptions);
            
            Console.WriteLine($"   ✅ GET /customers - Found {customers?.Length} customers");
            
            if (customers?.Length > 0)
            {
                var firstCustomer = customers[0];
                var customerId = firstCustomer.GetProperty("customerId").GetInt32();
                var email = firstCustomer.GetProperty("email").GetString();
                
                // Get specific customer
                response = await _httpClient.GetAsync($"/api/v1/customers/{customerId}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /customers/{customerId} - Customer details retrieved");
                
                // Get customer by email
                response = await _httpClient.GetAsync($"/api/v1/customers/by-email/{Uri.EscapeDataString(email!)}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /customers/by-email/{email} - Customer found by email");
                
                // Get customer order summary
                response = await _httpClient.GetAsync($"/api/v1/customers/{customerId}/order-summary");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /customers/{customerId}/order-summary - Order summary retrieved");
                
                // Get top customers
                response = await _httpClient.GetAsync("/api/v1/customers/top-customers?count=5");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /customers/top-customers - Top customers retrieved");
                
                // Get inactive customers
                response = await _httpClient.GetAsync("/api/v1/customers/inactive?daysBack=30");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /customers/inactive - Inactive customers retrieved");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Customers API test failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private static async Task TestOrdersEndpoints()
    {
        Console.WriteLine("🛒 Testing Orders API...");
        
        try
        {
            // Get all orders
            var response = await _httpClient.GetAsync("/api/v1/orders?pageSize=5");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var orders = JsonSerializer.Deserialize<JsonElement[]>(content, _jsonOptions);
            
            Console.WriteLine($"   ✅ GET /orders - Found {orders?.Length} orders");
            
            if (orders?.Length > 0)
            {
                var firstOrder = orders[0];
                var orderId = firstOrder.GetProperty("orderId").GetInt32();
                var customerId = firstOrder.GetProperty("customerId").GetInt32();
                
                // Get specific order
                response = await _httpClient.GetAsync($"/api/v1/orders/{orderId}");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /orders/{orderId} - Order details retrieved");
                
                // Get customer order history
                response = await _httpClient.GetAsync($"/api/v1/orders/customer/{customerId}/history");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /orders/customer/{customerId}/history - Order history retrieved");
                
                // Calculate order total
                response = await _httpClient.GetAsync($"/api/v1/orders/{orderId}/calculate-total");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /orders/{orderId}/calculate-total - Total calculated");
                
                // Get orders by status
                response = await _httpClient.GetAsync("/api/v1/orders?status=Delivered");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"   ✅ GET /orders?status=Delivered - Status filter works");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Orders API test failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    private static async Task TestAnalyticsEndpoints()
    {
        Console.WriteLine("📊 Testing Analytics Endpoints...");
        
        try
        {
            // Sales trends
            var response = await _httpClient.GetAsync("/api/v1/orders/analytics/sales-trends?daysBack=30");
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"   ✅ GET /orders/analytics/sales-trends - Sales trends retrieved");
            
            // Total sales
            response = await _httpClient.GetAsync("/api/v1/orders/analytics/total-sales");
            response.EnsureSuccessStatusCode();
            var totalSales = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   ✅ GET /orders/analytics/total-sales - Total: ${totalSales}");
            
            // Order count by status
            response = await _httpClient.GetAsync("/api/v1/orders/analytics/count-by-status?status=Delivered");
            response.EnsureSuccessStatusCode();
            var count = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   ✅ GET /orders/analytics/count-by-status - Delivered orders: {count}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Analytics API test failed: {ex.Message}");
        }
        
        Console.WriteLine();
    }
}