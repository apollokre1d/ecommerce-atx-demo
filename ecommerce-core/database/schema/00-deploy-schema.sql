-- =============================================
-- E-commerce Database Schema Deployment Script
-- SQL Server 2022 Complete Deployment
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
PRINT 'Starting schema deployment...';
PRINT '=============================================';
PRINT '';

-- =============================================
-- Step 1: Create Tables
-- =============================================
PRINT 'Step 1: Creating tables with SQL Server specific features...';
GO

:r "01-create-tables.sql"

PRINT '';
PRINT 'Step 1 completed: Tables created successfully.';
PRINT '';

-- =============================================
-- Step 2: Create Performance Indexes
-- =============================================
PRINT 'Step 2: Creating performance indexes...';
GO

:r "02-create-indexes.sql"

PRINT '';
PRINT 'Step 2 completed: Performance indexes created successfully.';
PRINT '';

-- =============================================
-- Step 3: Create Audit Infrastructure
-- =============================================
PRINT 'Step 3: Creating audit trail infrastructure...';
GO

:r "03-create-audit-table.sql"

PRINT '';
PRINT 'Step 3 completed: Audit infrastructure created successfully.';
PRINT '';

-- =============================================
-- Deployment Validation
-- =============================================
PRINT '=============================================';
PRINT 'Validating deployment...';
PRINT '=============================================';

-- Check table creation
DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*) 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_TYPE = 'BASE TABLE';

PRINT 'Tables created: ' + CAST(@TableCount AS NVARCHAR(10));

-- Check index creation
DECLARE @IndexCount INT;
SELECT @IndexCount = COUNT(*) 
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE o.schema_id = SCHEMA_ID('dbo')
  AND i.index_id > 0  -- Exclude heap tables
  AND i.is_primary_key = 0;  -- Exclude primary keys

PRINT 'Non-clustered indexes created: ' + CAST(@IndexCount AS NVARCHAR(10));

-- Check computed columns
DECLARE @ComputedColumnCount INT;
SELECT @ComputedColumnCount = COUNT(*)
FROM sys.computed_columns cc
INNER JOIN sys.objects o ON cc.object_id = o.object_id
WHERE o.schema_id = SCHEMA_ID('dbo');

PRINT 'Computed columns created: ' + CAST(@ComputedColumnCount AS NVARCHAR(10));

-- Check full-text indexes
DECLARE @FullTextIndexCount INT;
SELECT @FullTextIndexCount = COUNT(*)
FROM sys.fulltext_indexes fi
INNER JOIN sys.objects o ON fi.object_id = o.object_id
WHERE o.schema_id = SCHEMA_ID('dbo');

PRINT 'Full-text indexes created: ' + CAST(@FullTextIndexCount AS NVARCHAR(10));

-- List all created tables
PRINT '';
PRINT 'Created tables:';
SELECT 
    TABLE_NAME,
    CASE 
        WHEN TABLE_NAME LIKE '%Audit%' THEN 'Audit Infrastructure'
        WHEN TABLE_NAME IN ('Products', 'Categories', 'Customers', 'Orders', 'OrderItems') THEN 'Core E-commerce'
        ELSE 'Extended Features'
    END AS Category
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_TYPE = 'BASE TABLE'
ORDER BY Category, TABLE_NAME;

-- List SQL Server specific features
PRINT '';
PRINT 'SQL Server specific features implemented:';
PRINT '- Identity columns (IDENTITY(1,1))';
PRINT '- Computed columns (PERSISTED)';
PRINT '- SQL Server data types (DATETIME2, NVARCHAR(MAX), DECIMAL(18,2))';
PRINT '- Default value functions (GETUTCDATE(), SUSER_SNAME())';
PRINT '- Check constraints with complex logic';
PRINT '- Filtered indexes (WHERE clauses)';
PRINT '- Columnstore indexes';
PRINT '- Full-text search indexes';
PRINT '- XML data type and indexes';
PRINT '- HIERARCHYID data type';
PRINT '- GEOGRAPHY data type with spatial indexes';
PRINT '- Advanced audit trail with JSON support';

-- Performance baseline
PRINT '';
PRINT 'Performance baseline information:';
SELECT 
    'Database Size' AS Metric,
    CAST(SUM(size * 8.0 / 1024) AS DECIMAL(10,2)) AS ValueMB
FROM sys.database_files
WHERE type = 0  -- Data files only
UNION ALL
SELECT 
    'Log Size' AS Metric,
    CAST(SUM(size * 8.0 / 1024) AS DECIMAL(10,2)) AS ValueMB
FROM sys.database_files
WHERE type = 1;  -- Log files only

PRINT '';
PRINT '=============================================';
PRINT 'Schema deployment completed successfully!';
PRINT '=============================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Run stored procedure creation scripts';
PRINT '2. Create database triggers';
PRINT '3. Implement user-defined functions';
PRINT '4. Populate with sample data';
PRINT '5. Test ATX SQL transformation capabilities';
PRINT '';
PRINT 'Database is ready for ATX SQL assessment and transformation testing.';
GO