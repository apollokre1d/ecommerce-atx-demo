-- =============================================
-- COMPLETE E-COMMERCE DATABASE DEPLOYMENT
-- SQL Server 2022 - Single Script Deployment
-- For ATX SQL Transformation Testing
-- =============================================

-- Set database context
USE master;
GO

-- Check if database exists, create if not
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    PRINT 'Creating ECommerceDB database...';
    
    CREATE DATABASE ECommerceDB
    ON (
        NAME = 'ECommerceDB_Data',
        FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\ECommerceDB.mdf',
        SIZE = 1GB,
        MAXSIZE = 10GB,
        FILEGROWTH = 100MB
    )
    LOG ON (
        NAME = 'ECommerceDB_Log',
        FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\ECommerceDB.ldf',
        SIZE = 256MB,
        MAXSIZE = 2GB,
        FILEGROWTH = 64MB
    );
    
    PRINT 'ECommerceDB database created successfully.';
END
ELSE
BEGIN
    PRINT 'ECommerceDB database already exists.';
END
GO

-- Switch to the target database
USE ECommerceDB;
GO

-- Enable advanced features
PRINT 'Configuring database features...';

-- Enable full-text search if not already enabled
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ECommerceCatalog')
BEGIN
    CREATE FULLTEXT CATALOG ECommerceCatalog AS DEFAULT;
    PRINT 'Full-text catalog created.';
END
GO

-- Set database options for optimal performance and features
ALTER DATABASE ECommerceDB SET RECOVERY FULL;
ALTER DATABASE ECommerceDB SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE ECommerceDB SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE ECommerceDB SET AUTO_UPDATE_STATISTICS_ASYNC ON;
ALTER DATABASE ECommerceDB SET PARAMETERIZATION SIMPLE;
ALTER DATABASE ECommerceDB SET READ_COMMITTED_SNAPSHOT OFF;
ALTER DATABASE ECommerceDB SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE ECommerceDB SET PAGE_VERIFY CHECKSUM;
GO

PRINT 'Database configuration completed.';
PRINT '';
PRINT '=============================================';
PRINT 'Creating tables with SQL Server features...';
PRINT '=============================================';
PRINT '';-
- =============================================
-- TABLES CREATION
-- =============================================

-- Drop existing tables if they exist (for re-deployment)
IF OBJECT_ID('OrderItems', 'U') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('Products', 'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Customers', 'U') IS NOT NULL DROP TABLE Customers;
IF OBJECT_ID('AuditLog', 'U') IS NOT NULL DROP TABLE AuditLog;
IF OBJECT_ID('ProductSpecifications', 'U') IS NOT NULL DROP TABLE ProductSpecifications;
IF OBJECT_ID('OrganizationStructure', 'U') IS NOT NULL DROP TABLE OrganizationStructure;
IF OBJECT_ID('StoreLocations', 'U') IS NOT NULL DROP TABLE StoreLocations;
GO

-- Categories Table with Hierarchical Structure
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ParentCategoryId INT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2(7) NULL,
    
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (CategoryId),
    CONSTRAINT FK_Categories_ParentCategory 
        FOREIGN KEY (ParentCategoryId) 
        REFERENCES Categories(CategoryId),
    CONSTRAINT CK_Categories_Name_NotEmpty 
        CHECK (LEN(TRIM(Name)) > 0),
    CONSTRAINT CK_Categories_DisplayOrder_NonNegative 
        CHECK (DisplayOrder >= 0)
);
GO

-- Products Table with Computed Columns
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CategoryId INT NOT NULL,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2(7) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- SQL Server Computed Columns
    SearchVector AS (Name + ' ' + ISNULL(Description, '')) PERSISTED,
    PriceCategory AS (
        CASE 
            WHEN Price < 50.00 THEN 'Budget'
            WHEN Price < 200.00 THEN 'Standard'
            WHEN Price < 500.00 THEN 'Premium'
            ELSE 'Luxury'
        END
    ) PERSISTED,
    
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT FK_Products_Categories 
        FOREIGN KEY (CategoryId) 
        REFERENCES Categories(CategoryId),
    CONSTRAINT CK_Products_Name_NotEmpty 
        CHECK (LEN(TRIM(Name)) > 0),
    CONSTRAINT CK_Products_Price_Positive 
        CHECK (Price > 0),
    CONSTRAINT CK_Products_Price_Reasonable 
        CHECK (Price <= 999999.99)
);
GO

-- Customers Table
CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20) NULL,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2(7) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customers_Email UNIQUE (Email),
    CONSTRAINT CK_Customers_FirstName_NotEmpty 
        CHECK (LEN(TRIM(FirstName)) > 0),
    CONSTRAINT CK_Customers_LastName_NotEmpty 
        CHECK (LEN(TRIM(LastName)) > 0),
    CONSTRAINT CK_Customers_Email_Format 
        CHECK (Email LIKE '%@%.%' AND LEN(Email) > 5)
);
GO

-- Orders Table
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) NOT NULL,
    CustomerId INT NOT NULL,
    OrderDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    ShippingAddress NVARCHAR(500) NULL,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2(7) NULL,
    
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT FK_Orders_Customers 
        FOREIGN KEY (CustomerId) 
        REFERENCES Customers(CustomerId),
    CONSTRAINT CK_Orders_TotalAmount_Positive 
        CHECK (TotalAmount > 0),
    CONSTRAINT CK_Orders_TaxAmount_NonNegative 
        CHECK (TaxAmount >= 0),
    CONSTRAINT CK_Orders_Status_Valid 
        CHECK (Status IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded'))
);
GO

-- OrderItems Table with Computed LineTotal
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) NOT NULL,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal AS (Quantity * UnitPrice) PERSISTED,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (OrderItemId),
    CONSTRAINT FK_OrderItems_Orders 
        FOREIGN KEY (OrderId) 
        REFERENCES Orders(OrderId) 
        ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products 
        FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId),
    CONSTRAINT CK_OrderItems_Quantity_Positive 
        CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice_Positive 
        CHECK (UnitPrice > 0),
    CONSTRAINT UQ_OrderItems_OrderId_ProductId 
        UNIQUE (OrderId, ProductId)
);
GO

-- AuditLog Table
CREATE TABLE AuditLog (
    AuditLogId INT IDENTITY(1,1) NOT NULL,
    TableName NVARCHAR(50) NOT NULL,
    Action NVARCHAR(10) NOT NULL,
    RecordId INT NULL,
    UserId NVARCHAR(128) NOT NULL DEFAULT SUSER_SNAME(),
    Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED (AuditLogId),
    CONSTRAINT CK_AuditLog_Action_Valid 
        CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE'))
);
GO

-- SQL Server Specific Tables
CREATE TABLE ProductSpecifications (
    SpecificationId INT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    SpecificationXML XML NOT NULL,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_ProductSpecifications PRIMARY KEY (SpecificationId),
    CONSTRAINT FK_ProductSpecifications_Products 
        FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) 
        ON DELETE CASCADE
);
GO

CREATE TABLE OrganizationStructure (
    NodeId INT IDENTITY(1,1) NOT NULL,
    OrgNode HIERARCHYID NOT NULL,
    NodeLevel AS OrgNode.GetLevel(),
    DepartmentName NVARCHAR(100) NOT NULL,
    ManagerId INT NULL,
    
    CONSTRAINT PK_OrganizationStructure PRIMARY KEY (NodeId),
    CONSTRAINT UQ_OrganizationStructure_OrgNode UNIQUE (OrgNode)
);
GO

CREATE TABLE StoreLocations (
    StoreId INT IDENTITY(1,1) NOT NULL,
    StoreName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    Location GEOGRAPHY NULL,
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_StoreLocations PRIMARY KEY (StoreId)
);
GO

PRINT 'Tables created successfully!';
PRINT '';
PRINT '=============================================';
PRINT 'Creating performance indexes...';
PRINT '=============================================';

-- Performance Indexes
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_AuditLog_Columnstore
ON AuditLog (TableName, Action, Timestamp, UserId);

CREATE NONCLUSTERED INDEX IX_Products_Active_Price
ON Products (Price, CategoryId)
WHERE IsActive = 1;

CREATE NONCLUSTERED INDEX IX_Orders_Customer_Status
ON Orders (CustomerId, Status)
INCLUDE (OrderDate, TotalAmount, TaxAmount);

CREATE FULLTEXT INDEX ON Products (Name, Description)
KEY INDEX PK_Products
ON ECommerceCatalog;

PRINT 'Indexes created successfully!';
PRINT '';
PRINT '=============================================';
PRINT 'DEPLOYMENT COMPLETED SUCCESSFULLY!';
PRINT '=============================================';
PRINT '';
PRINT 'Database: ECommerceDB';
PRINT 'Tables: 8 tables created with SQL Server specific features';
PRINT 'Features: Computed columns, XML, HIERARCHYID, GEOGRAPHY, Full-text search';
PRINT 'Indexes: Columnstore, filtered, and full-text indexes created';
PRINT '';
PRINT 'Ready for Task 4: T-SQL Stored Procedures implementation';
GO