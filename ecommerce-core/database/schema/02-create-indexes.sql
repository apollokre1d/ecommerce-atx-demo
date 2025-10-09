-- =============================================
-- Performance Indexes and Constraints
-- SQL Server 2022 Optimization Features
-- For ATX SQL Transformation Testing
-- =============================================

USE ECommerceDB;
GO

PRINT 'Creating performance indexes and constraints...';

-- =============================================
-- Categories Table Indexes
-- =============================================

-- Index for hierarchical queries
CREATE NONCLUSTERED INDEX IX_Categories_ParentCategoryId
ON Categories (ParentCategoryId)
INCLUDE (Name, DisplayOrder, IsActive)
WHERE ParentCategoryId IS NOT NULL;
GO

-- Index for active categories ordered by display order
CREATE NONCLUSTERED INDEX IX_Categories_Active_DisplayOrder
ON Categories (DisplayOrder, Name)
WHERE IsActive = 1;
GO

-- Index for category name searches (case-insensitive)
CREATE NONCLUSTERED INDEX IX_Categories_Name
ON Categories (Name);
GO

-- =============================================
-- Products Table Indexes
-- =============================================

-- Composite index for category and price filtering
CREATE NONCLUSTERED INDEX IX_Products_CategoryId_Price
ON Products (CategoryId, Price DESC)
INCLUDE (Name, IsActive, CreatedDate)
WHERE IsActive = 1;
GO

-- Index for product name searches with full-text support
CREATE NONCLUSTERED INDEX IX_Products_Name_Active
ON Products (Name)
INCLUDE (Description, Price, CategoryId)
WHERE IsActive = 1;
GO

-- Index for price range queries
CREATE NONCLUSTERED INDEX IX_Products_Price_Range
ON Products (Price, PriceCategory)
INCLUDE (Name, CategoryId)
WHERE IsActive = 1;
GO

-- Index for recently added products
CREATE NONCLUSTERED INDEX IX_Products_CreatedDate_Desc
ON Products (CreatedDate DESC)
INCLUDE (Name, Price, CategoryId)
WHERE IsActive = 1;
GO

-- Covering index for search vector (computed column)
CREATE NONCLUSTERED INDEX IX_Products_SearchVector
ON Products (SearchVector)
INCLUDE (ProductId, Name, Price, CategoryId)
WHERE IsActive = 1;
GO

-- =============================================
-- Customers Table Indexes
-- =============================================

-- Index for customer name searches
CREATE NONCLUSTERED INDEX IX_Customers_Name
ON Customers (LastName, FirstName)
INCLUDE (Email, Phone, CreatedDate)
WHERE IsActive = 1;
GO

-- Index for email domain analysis
CREATE NONCLUSTERED INDEX IX_Customers_Email_Domain
ON Customers (Email)
INCLUDE (FirstName, LastName, CreatedDate)
WHERE IsActive = 1;
GO

-- Index for customer registration date analysis
CREATE NONCLUSTERED INDEX IX_Customers_CreatedDate
ON Customers (CreatedDate DESC)
INCLUDE (FirstName, LastName, Email)
WHERE IsActive = 1;
GO

-- =============================================
-- Orders Table Indexes
-- =============================================

-- Primary index for customer order history
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDate
ON Orders (CustomerId, OrderDate DESC)
INCLUDE (OrderId, TotalAmount, TaxAmount, Status);
GO

-- Index for order status tracking
CREATE NONCLUSTERED INDEX IX_Orders_Status_OrderDate
ON Orders (Status, OrderDate DESC)
INCLUDE (CustomerId, TotalAmount);
GO

-- Index for order value analysis
CREATE NONCLUSTERED INDEX IX_Orders_TotalAmount_Desc
ON Orders (TotalAmount DESC)
INCLUDE (CustomerId, OrderDate, Status)
WHERE Status NOT IN ('Cancelled', 'Refunded');
GO

-- Index for date range queries
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate_Range
ON Orders (OrderDate)
INCLUDE (CustomerId, TotalAmount, TaxAmount, Status);
GO

-- Filtered index for pending orders
CREATE NONCLUSTERED INDEX IX_Orders_Pending
ON Orders (OrderDate DESC)
INCLUDE (CustomerId, TotalAmount)
WHERE Status = 'Pending';
GO

-- =============================================
-- OrderItems Table Indexes
-- =============================================

-- Primary index for order details
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId
ON OrderItems (OrderId)
INCLUDE (ProductId, Quantity, UnitPrice, LineTotal);
GO

-- Index for product sales analysis
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId_Quantity
ON OrderItems (ProductId, Quantity DESC)
INCLUDE (OrderId, UnitPrice, LineTotal, CreatedDate);
GO

-- Composite index for order and product lookup
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId_ProductId
ON OrderItems (OrderId, ProductId)
INCLUDE (Quantity, UnitPrice, LineTotal);
GO

-- Index for line total analysis
CREATE NONCLUSTERED INDEX IX_OrderItems_LineTotal_Desc
ON OrderItems (LineTotal DESC)
INCLUDE (OrderId, ProductId, Quantity);
GO

-- =============================================
-- AuditLog Table Indexes
-- =============================================

-- Primary index for audit queries by table and date
CREATE NONCLUSTERED INDEX IX_AuditLog_TableName_Timestamp
ON AuditLog (TableName, Timestamp DESC)
INCLUDE (Action, RecordId, UserId);
GO

-- Index for user activity tracking
CREATE NONCLUSTERED INDEX IX_AuditLog_UserId_Timestamp
ON AuditLog (UserId, Timestamp DESC)
INCLUDE (TableName, Action, RecordId);
GO

-- Index for record-specific audit trail
CREATE NONCLUSTERED INDEX IX_AuditLog_TableName_RecordId
ON AuditLog (TableName, RecordId, Timestamp DESC)
INCLUDE (Action, UserId)
WHERE RecordId IS NOT NULL;
GO

-- Filtered index for recent audit entries
CREATE NONCLUSTERED INDEX IX_AuditLog_Recent
ON AuditLog (Timestamp DESC)
INCLUDE (TableName, Action, RecordId, UserId)
WHERE Timestamp >= DATEADD(DAY, -30, GETUTCDATE());
GO

-- =============================================
-- Additional SQL Server Specific Indexes
-- =============================================

-- XML index for ProductSpecifications (SQL Server specific)
CREATE PRIMARY XML INDEX IX_ProductSpecifications_XML_Primary
ON ProductSpecifications (SpecificationXML);
GO

CREATE XML INDEX IX_ProductSpecifications_XML_Path
ON ProductSpecifications (SpecificationXML)
USING XML INDEX IX_ProductSpecifications_XML_Primary
FOR PATH;
GO

-- Spatial index for StoreLocations (SQL Server specific)
CREATE SPATIAL INDEX IX_StoreLocations_Location_Spatial
ON StoreLocations (Location)
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO

-- Columnstore index for analytics (SQL Server specific)
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_Orders_Analytics_Columnstore
ON Orders (CustomerId, OrderDate, TotalAmount, TaxAmount, Status);
GO

-- Memory-optimized table index (SQL Server specific - commented out as requires special setup)
/*
-- This would require enabling In-Memory OLTP
CREATE NONCLUSTERED INDEX IX_Products_Memory_Optimized
ON Products (CategoryId, Price)
WITH (BUCKET_COUNT = 1000);
*/

-- =============================================
-- Statistics Creation for Query Optimization
-- =============================================

-- Create statistics for better query plans
CREATE STATISTICS ST_Products_Price_Category
ON Products (Price, CategoryId)
WHERE IsActive = 1;
GO

CREATE STATISTICS ST_Orders_Customer_Amount
ON Orders (CustomerId, TotalAmount)
WHERE Status NOT IN ('Cancelled', 'Refunded');
GO

CREATE STATISTICS ST_OrderItems_Product_Quantity
ON OrderItems (ProductId, Quantity);
GO

-- =============================================
-- Index Maintenance Views (SQL Server specific)
-- =============================================

-- View to monitor index fragmentation
CREATE VIEW vw_IndexFragmentation
AS
SELECT 
    OBJECT_SCHEMA_NAME(ips.object_id) AS SchemaName,
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.index_type_desc,
    ips.avg_fragmentation_in_percent,
    ips.page_count,
    ips.record_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
  AND ips.page_count > 100;
GO

-- View to monitor index usage
CREATE VIEW vw_IndexUsage
AS
SELECT 
    OBJECT_SCHEMA_NAME(ius.object_id) AS SchemaName,
    OBJECT_NAME(ius.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.user_updates,
    ius.last_user_seek,
    ius.last_user_scan,
    ius.last_user_lookup
FROM sys.dm_db_index_usage_stats ius
INNER JOIN sys.indexes i ON ius.object_id = i.object_id AND ius.index_id = i.index_id
WHERE ius.database_id = DB_ID()
  AND OBJECT_SCHEMA_NAME(ius.object_id) = 'dbo';
GO

PRINT 'Performance indexes created successfully!';
PRINT 'Created indexes for: Categories, Products, Customers, Orders, OrderItems, AuditLog';
PRINT 'SQL Server specific features: Filtered indexes, Columnstore indexes, XML indexes, Spatial indexes';
PRINT 'Monitoring views: vw_IndexFragmentation, vw_IndexUsage';
GO