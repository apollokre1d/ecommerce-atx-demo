# 🧪 E-Commerce API Testing Guide

## 📋 Prerequisites

1. **SQL Server Database** - Ensure your SQL Server is running with sample data
2. **.NET 8 SDK** - Required to build and run the API
3. **Sample Data** - Run `SAMPLE-DATA-COMPLETE.sql` first

## 🚀 Quick Start

### Option 1: Automated Testing (Recommended)
```powershell
# Run from the ecommerce-core directory
.\run-api-tests.ps1
```

This script will:
- Build the API and test client
- Start the API on port 5000
- Run comprehensive tests
- Display results and clean up

### Option 2: Manual Testing

#### Step 1: Add Sample Data
1. Connect to SQL Server via RDP (`3.67.133.184:3389`)
2. Open SSMS and run `SAMPLE-DATA-COMPLETE.sql`

#### Step 2: Start the API
```bash
cd src/ECommerce.API
dotnet run --urls "http://localhost:5000"
```

#### Step 3: Run Tests
```bash
cd tests/ECommerce.API.TestClient
dotnet run
```

## 📊 What Gets Tested

### 🏗️ **API Infrastructure**
- ✅ API connectivity and health checks
- ✅ Swagger documentation generation
- ✅ Database connection validation
- ✅ Error handling and logging

### 📂 **Categories API** (`/api/v1/categories`)
- ✅ Get all categories (hierarchical structure)
- ✅ Get category by ID
- ✅ Get products in category (uses stored procedure)
- ✅ Get category statistics

### 📦 **Products API** (`/api/v1/products`)
- ✅ Get products with pagination
- ✅ Search products (full-text search)
- ✅ Filter by category (stored procedure)
- ✅ Filter by price range
- ✅ Get top selling products
- ✅ Get sales analysis (SQL Server function)
- ✅ Product CRUD operations

### 👥 **Customers API** (`/api/v1/customers`)
- ✅ Get customers with filtering
- ✅ Find customer by email
- ✅ Get customer order summary (SQL Server function)
- ✅ Get customer loyalty level (SQL Server function)
- ✅ Get top customers by value
- ✅ Get inactive customers
- ✅ Customer CRUD operations

### 🛒 **Orders API** (`/api/v1/orders`)
- ✅ Get orders with filtering
- ✅ Get order details with items
- ✅ Create order (uses stored procedure)
- ✅ Update order status
- ✅ Cancel orders
- ✅ Get customer order history (stored procedure)
- ✅ Calculate order totals (SQL Server function)

### 📊 **Analytics Endpoints**
- ✅ Sales trends analysis
- ✅ Total sales calculations
- ✅ Order counts by status
- ✅ Customer analytics
- ✅ Product performance metrics

## 🔧 Configuration

### Connection Strings
The API uses these connection strings (in order of preference):

1. **Production**: `Server=3.67.133.184,1433;Database=ECommerceDB;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;`
2. **Local Development**: `Server=localhost,1433;Database=ECommerceDB;Trusted_Connection=true;TrustServerCertificate=true;`

### API Endpoints
- **Base URL**: `http://localhost:5000`
- **Swagger UI**: `http://localhost:5000`
- **Health Check**: `http://localhost:5000/health`
- **API Info**: `http://localhost:5000/api/info`

## 📈 Expected Test Results

### ✅ **Successful Test Output**
```
🚀 E-Commerce API Test Client
=====================================

🔍 Testing API connectivity...
✅ Connected to API: http://localhost:5000/
   Environment: Development

🧪 Running comprehensive API tests...

📂 Testing Categories API...
   ✅ GET /categories - Found 16 categories
   ✅ GET /categories/1 - Category details retrieved
   ✅ GET /categories/1/products - Products retrieved
   ✅ GET /categories/1/statistics - Statistics retrieved

📦 Testing Products API...
   ✅ GET /products - Found 5 products (page 1)
   ✅ GET /products/1 - Product details retrieved
   ✅ GET /products?searchTerm=laptop - Search completed
   ✅ GET /products?categoryId=6 - Category filter works
   ✅ GET /products/top-selling - Top sellers retrieved
   ✅ GET /products/sales-analysis - Analytics retrieved

👥 Testing Customers API...
   ✅ GET /customers - Found 5 customers
   ✅ GET /customers/1 - Customer details retrieved
   ✅ GET /customers/by-email/john.smith@email.com - Customer found by email
   ✅ GET /customers/1/order-summary - Order summary retrieved
   ✅ GET /customers/top-customers - Top customers retrieved
   ✅ GET /customers/inactive - Inactive customers retrieved

🛒 Testing Orders API...
   ✅ GET /orders - Found 5 orders
   ✅ GET /orders/1 - Order details retrieved
   ✅ GET /orders/customer/1/history - Order history retrieved
   ✅ GET /orders/1/calculate-total - Total calculated
   ✅ GET /orders?status=Delivered - Status filter works

📊 Testing Analytics Endpoints...
   ✅ GET /orders/analytics/sales-trends - Sales trends retrieved
   ✅ GET /orders/analytics/total-sales - Total: $12,345.67
   ✅ GET /orders/analytics/count-by-status - Delivered orders: 8

✅ All tests completed successfully!
```

## 🐛 Troubleshooting

### Common Issues

#### ❌ **Database Connection Failed**
```
Error: A network-related or instance-specific error occurred
```
**Solution**: 
- Verify SQL Server is running at `3.67.133.184:1433`
- Check connection string in `appsettings.json`
- Ensure sample data has been loaded

#### ❌ **Port Already in Use**
```
Error: Port 5000 is already in use
```
**Solution**:
- Stop existing API instances
- Use different port: `dotnet run --urls "http://localhost:5001"`
- Kill process: `netstat -ano | findstr :5000` then `taskkill /PID <PID> /F`

#### ❌ **Sample Data Missing**
```
Error: No products found
```
**Solution**:
- Run `SAMPLE-DATA-COMPLETE.sql` in SSMS
- Verify data: `SELECT COUNT(*) FROM Products`

#### ❌ **Stored Procedure Errors**
```
Error: Could not find stored procedure 'GetProductsByCategory'
```
**Solution**:
- Run `TASK-4-STORED-PROCEDURES.sql` in SSMS
- Verify procedures: `SELECT name FROM sys.procedures`

## 📊 Performance Expectations

### Response Times (Local Testing)
- **Simple GET requests**: < 100ms
- **Search operations**: < 200ms
- **Stored procedures**: < 300ms
- **Analytics queries**: < 500ms

### Throughput
- **Concurrent users**: 50+
- **Requests per second**: 100+
- **Database connections**: Pooled efficiently

## 🎯 Next Steps

After successful testing:

1. **Deploy to AWS** - Use ECS/EC2 deployment guides
2. **ATX SQL Testing** - Transform to PostgreSQL
3. **Performance Testing** - Load testing with realistic data
4. **Security Testing** - Authentication and authorization
5. **Integration Testing** - End-to-end workflow validation

## 📞 Support

If you encounter issues:
1. Check the console output for detailed error messages
2. Review the API logs in the `logs/` directory
3. Verify database connectivity and sample data
4. Ensure all prerequisites are installed