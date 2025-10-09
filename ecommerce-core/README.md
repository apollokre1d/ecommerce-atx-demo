# E-commerce Core Application

## Overview

This is a comprehensive .NET e-commerce application designed to test AWS Transform for SQL Server (ATX SQL) capabilities. The application demonstrates realistic SQL Server dependencies and complex database features to validate transformation effectiveness.

## Architecture

- **ECommerce.Web** - ASP.NET Core MVC frontend application
- **ECommerce.API** - RESTful Web API for business operations
- **ECommerce.Data** - Entity Framework Core data access layer

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server 2019/2022 (local or remote instance)
- Visual Studio 2022 or VS Code with C# extension
- Git for version control

## Getting Started

### 1. Clone and Setup

```bash
git clone <repository-url>
cd ecommerce-core
```

### 2. Database Setup

1. Ensure SQL Server is running
2. Update connection strings in `appsettings.json` files
3. Run database migrations:

```bash
dotnet ef database update --project src/ECommerce.Data
```

### 3. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run Web application
dotnet run --project src/ECommerce.Web

# Run API (in separate terminal)
dotnet run --project src/ECommerce.API
```

## Project Structure

```
src/
├── ECommerce.Web/           # MVC Web Application
├── ECommerce.API/           # Web API
└── ECommerce.Data/          # Entity Framework Models

database/
├── schema/                  # Table creation scripts
├── stored-procedures/       # T-SQL stored procedures
├── triggers/               # Database triggers
├── functions/              # User-defined functions
└── sample-data/            # Test data scripts

deployment/
├── docker/                 # Docker configurations
└── github-actions/         # CI/CD workflows

tests/
├── unit/                   # Unit tests
└── integration/            # Integration tests

docs/
├── api/                    # API documentation
└── setup/                  # Setup guides
```

## SQL Server Features Tested

This application includes SQL Server-specific features to thoroughly test ATX SQL transformation:

- **Identity Columns** - Auto-incrementing primary keys
- **Computed Columns** - Calculated fields (SearchVector, LineTotal)
- **Stored Procedures** - Complex T-SQL business logic
- **Triggers** - Audit trail implementation
- **User-Defined Functions** - Business calculations
- **Complex Data Types** - DATETIME2, NVARCHAR(MAX), DECIMAL(18,2)
- **JSON Operations** - Modern SQL Server JSON functions

## ATX SQL Testing

This application is specifically designed to validate:

1. **Repository Discovery** - Multi-project .NET solution analysis
2. **Schema Assessment** - SQL Server feature complexity analysis
3. **Code Transformation** - Entity Framework provider conversion
4. **Data Migration** - SQL Server to PostgreSQL migration
5. **Deployment Automation** - Container and cloud deployment

## Configuration

### Connection Strings

Update `appsettings.json` in each project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ECommerceDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Environment Variables

For production deployment:

- `ECOMMERCE_DB_CONNECTION` - Database connection string
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `ASPNETCORE_URLS` - Application URLs

## Security Considerations

- Connection strings use integrated security by default
- No hardcoded credentials in source code
- Environment-specific configuration files excluded from Git
- Input validation and SQL injection prevention implemented
- HTTPS enforced in production

## Testing

```bash
# Run unit tests
dotnet test tests/unit/

# Run integration tests
dotnet test tests/integration/

# Run all tests
dotnet test
```

## Docker Support

```bash
# Build container
docker build -t ecommerce-core .

# Run with docker-compose
docker-compose up -d
```

## Contributing

1. Follow .NET coding standards
2. Include unit tests for new features
3. Update documentation for API changes
4. Ensure security best practices

## ATX SQL Transformation Results

This section will be updated with transformation results:

- [ ] Assessment completion
- [ ] Schema conversion success rate
- [ ] Application code transformation
- [ ] Data migration validation
- [ ] Performance comparison

## License

This project is for AWS Transform for SQL Server testing purposes.