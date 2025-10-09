-- =============================================
-- Audit Trail Infrastructure
-- SQL Server 2022 Advanced Auditing Features
-- For ATX SQL Transformation Testing
-- =============================================

USE ECommerceDB;
GO

PRINT 'Creating audit trail infrastructure...';

-- =============================================
-- Enhanced Audit Log Table
-- =============================================

-- Drop existing audit table if it exists
IF OBJECT_ID('AuditLogDetailed', 'U') IS NOT NULL 
    DROP TABLE AuditLogDetailed;
GO

CREATE TABLE AuditLogDetailed (
    AuditLogId BIGINT IDENTITY(1,1) NOT NULL,
    
    -- Basic audit information
    TableName NVARCHAR(128) NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL DEFAULT 'dbo',
    Action NVARCHAR(10) NOT NULL,
    RecordId INT NULL,
    
    -- User and session information
    UserId NVARCHAR(128) NOT NULL DEFAULT SUSER_SNAME(),
    UserSid VARBINARY(85) NOT NULL DEFAULT SUSER_SID(),
    SessionId INT NOT NULL DEFAULT @@SPID,
    ApplicationName NVARCHAR(128) NULL DEFAULT APP_NAME(),
    HostName NVARCHAR(128) NULL DEFAULT HOST_NAME(),
    
    -- Timing information
    Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    TransactionId BIGINT NULL DEFAULT @@TRANCOUNT,
    
    -- Change tracking
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    ChangedColumns NVARCHAR(MAX) NULL,
    
    -- SQL Server specific audit fields
    DatabaseName NVARCHAR(128) NOT NULL DEFAULT DB_NAME(),
    ServerName NVARCHAR(128) NOT NULL DEFAULT @@SERVERNAME,
    
    -- Additional metadata
    AuditVersion TINYINT NOT NULL DEFAULT 1,
    IsSystemGenerated BIT NOT NULL DEFAULT 0,
    
    -- Computed columns for analysis
    AuditDate AS CAST(Timestamp AS DATE) PERSISTED,
    AuditHour AS DATEPART(HOUR, Timestamp) PERSISTED,
    
    -- Primary Key
    CONSTRAINT PK_AuditLogDetailed PRIMARY KEY CLUSTERED (AuditLogId),
    
    -- Check Constraints
    CONSTRAINT CK_AuditLogDetailed_Action_Valid 
        CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE', 'MERGE')),
    CONSTRAINT CK_AuditLogDetailed_TableName_NotEmpty 
        CHECK (LEN(TRIM(TableName)) > 0),
    CONSTRAINT CK_AuditLogDetailed_AuditVersion_Valid 
        CHECK (AuditVersion BETWEEN 1 AND 255)
);
GO

-- =============================================
-- Audit Configuration Table
-- =============================================

CREATE TABLE AuditConfiguration (
    ConfigId INT IDENTITY(1,1) NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL DEFAULT 'dbo',
    IsAuditEnabled BIT NOT NULL DEFAULT 1,
    AuditInserts BIT NOT NULL DEFAULT 1,
    AuditUpdates BIT NOT NULL DEFAULT 1,
    AuditDeletes BIT NOT NULL DEFAULT 1,
    
    -- Column-level audit configuration
    ColumnsToAudit NVARCHAR(MAX) NULL, -- JSON array of column names
    ColumnsToExclude NVARCHAR(MAX) NULL, -- JSON array of column names to exclude
    
    -- Retention policy
    RetentionDays INT NOT NULL DEFAULT 365,
    
    -- Metadata
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2(7) NULL,
    CreatedBy NVARCHAR(128) NOT NULL DEFAULT SUSER_SNAME(),
    
    CONSTRAINT PK_AuditConfiguration PRIMARY KEY (ConfigId),
    CONSTRAINT UQ_AuditConfiguration_Table UNIQUE (SchemaName, TableName),
    CONSTRAINT CK_AuditConfiguration_RetentionDays_Positive 
        CHECK (RetentionDays > 0)
);
GO

-- =============================================
-- Audit Statistics Table
-- =============================================

CREATE TABLE AuditStatistics (
    StatId BIGINT IDENTITY(1,1) NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    Action NVARCHAR(10) NOT NULL,
    AuditDate DATE NOT NULL,
    RecordCount INT NOT NULL DEFAULT 0,
    
    -- Performance metrics
    AvgProcessingTimeMs DECIMAL(10,3) NULL,
    MaxProcessingTimeMs DECIMAL(10,3) NULL,
    TotalDataSizeKB DECIMAL(15,3) NULL,
    
    -- Computed timestamp
    CreatedTimestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_AuditStatistics PRIMARY KEY (StatId),
    CONSTRAINT UQ_AuditStatistics_Table_Action_Date 
        UNIQUE (TableName, Action, AuditDate)
);
GO

-- =============================================
-- Audit Archive Table (for old records)
-- =============================================

CREATE TABLE AuditLogArchive (
    ArchiveId BIGINT IDENTITY(1,1) NOT NULL,
    OriginalAuditLogId BIGINT NOT NULL,
    
    -- Copy of original audit data
    TableName NVARCHAR(128) NOT NULL,
    Action NVARCHAR(10) NOT NULL,
    RecordId INT NULL,
    UserId NVARCHAR(128) NOT NULL,
    Timestamp DATETIME2(7) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    
    -- Archive metadata
    ArchivedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ArchivedBy NVARCHAR(128) NOT NULL DEFAULT SUSER_SNAME(),
    ArchiveReason NVARCHAR(100) NOT NULL DEFAULT 'Retention Policy',
    
    CONSTRAINT PK_AuditLogArchive PRIMARY KEY (ArchiveId)
);
GO

-- =============================================
-- Indexes for Audit Tables
-- =============================================

-- Primary audit log indexes
CREATE NONCLUSTERED INDEX IX_AuditLogDetailed_TableName_Timestamp
ON AuditLogDetailed (TableName, Timestamp DESC)
INCLUDE (Action, RecordId, UserId);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogDetailed_RecordId_TableName
ON AuditLogDetailed (RecordId, TableName, Timestamp DESC)
WHERE RecordId IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_AuditLogDetailed_UserId_Timestamp
ON AuditLogDetailed (UserId, Timestamp DESC)
INCLUDE (TableName, Action, RecordId);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogDetailed_AuditDate
ON AuditLogDetailed (AuditDate, AuditHour)
INCLUDE (TableName, Action, UserId);
GO

-- Columnstore index for analytics
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_AuditLogDetailed_Analytics
ON AuditLogDetailed (TableName, Action, AuditDate, AuditHour, UserId, RecordId);
GO

-- Configuration table indexes
CREATE NONCLUSTERED INDEX IX_AuditConfiguration_TableName
ON AuditConfiguration (SchemaName, TableName)
WHERE IsAuditEnabled = 1;
GO

-- Statistics table indexes
CREATE NONCLUSTERED INDEX IX_AuditStatistics_Date_Table
ON AuditStatistics (AuditDate DESC, TableName)
INCLUDE (Action, RecordCount);
GO

-- Archive table indexes
CREATE NONCLUSTERED INDEX IX_AuditLogArchive_OriginalId
ON AuditLogArchive (OriginalAuditLogId);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogArchive_ArchivedDate
ON AuditLogArchive (ArchivedDate DESC)
INCLUDE (TableName, Action);
GO

-- =============================================
-- Audit Views for Reporting
-- =============================================

-- View for current audit activity
CREATE VIEW vw_AuditActivity
AS
SELECT 
    TableName,
    Action,
    COUNT(*) AS RecordCount,
    MIN(Timestamp) AS FirstActivity,
    MAX(Timestamp) AS LastActivity,
    COUNT(DISTINCT UserId) AS UniqueUsers,
    COUNT(DISTINCT CAST(Timestamp AS DATE)) AS ActiveDays
FROM AuditLogDetailed
WHERE Timestamp >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY TableName, Action;
GO

-- View for user audit summary
CREATE VIEW vw_UserAuditSummary
AS
SELECT 
    UserId,
    COUNT(*) AS TotalActions,
    COUNT(DISTINCT TableName) AS TablesAccessed,
    MIN(Timestamp) AS FirstActivity,
    MAX(Timestamp) AS LastActivity,
    SUM(CASE WHEN Action = 'INSERT' THEN 1 ELSE 0 END) AS Inserts,
    SUM(CASE WHEN Action = 'UPDATE' THEN 1 ELSE 0 END) AS Updates,
    SUM(CASE WHEN Action = 'DELETE' THEN 1 ELSE 0 END) AS Deletes
FROM AuditLogDetailed
WHERE Timestamp >= DATEADD(DAY, -90, GETUTCDATE())
GROUP BY UserId;
GO

-- View for table audit summary
CREATE VIEW vw_TableAuditSummary
AS
SELECT 
    TableName,
    COUNT(*) AS TotalChanges,
    COUNT(DISTINCT UserId) AS UniqueUsers,
    COUNT(DISTINCT RecordId) AS UniqueRecords,
    AVG(DATALENGTH(OldValues) + DATALENGTH(NewValues)) AS AvgChangeSize,
    MIN(Timestamp) AS FirstChange,
    MAX(Timestamp) AS LastChange
FROM AuditLogDetailed
WHERE RecordId IS NOT NULL
  AND Timestamp >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY TableName;
GO

-- =============================================
-- Audit Maintenance Procedures
-- =============================================

-- Procedure to clean up old audit records
CREATE PROCEDURE sp_CleanupAuditLog
    @RetentionDays INT = 365,
    @BatchSize INT = 10000,
    @ArchiveBeforeDelete BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    DECLARE @RowsProcessed INT = 0;
    DECLARE @TotalRowsDeleted INT = 0;
    
    PRINT 'Starting audit log cleanup for records older than ' + CAST(@CutoffDate AS NVARCHAR(50));
    
    WHILE 1 = 1
    BEGIN
        BEGIN TRANSACTION;
        
        -- Archive records if requested
        IF @ArchiveBeforeDelete = 1
        BEGIN
            INSERT INTO AuditLogArchive (
                OriginalAuditLogId, TableName, Action, RecordId, 
                UserId, Timestamp, OldValues, NewValues
            )
            SELECT TOP (@BatchSize)
                AuditLogId, TableName, Action, RecordId,
                UserId, Timestamp, OldValues, NewValues
            FROM AuditLogDetailed
            WHERE Timestamp < @CutoffDate
              AND AuditLogId NOT IN (SELECT OriginalAuditLogId FROM AuditLogArchive);
            
            SET @RowsProcessed = @@ROWCOUNT;
        END
        
        -- Delete old records
        DELETE TOP (@BatchSize)
        FROM AuditLogDetailed
        WHERE Timestamp < @CutoffDate;
        
        SET @RowsProcessed = @@ROWCOUNT;
        SET @TotalRowsDeleted = @TotalRowsDeleted + @RowsProcessed;
        
        COMMIT TRANSACTION;
        
        PRINT 'Processed batch: ' + CAST(@RowsProcessed AS NVARCHAR(10)) + ' records';
        
        -- Exit if no more records to process
        IF @RowsProcessed = 0
            BREAK;
            
        -- Small delay to avoid blocking
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Audit log cleanup completed. Total records deleted: ' + CAST(@TotalRowsDeleted AS NVARCHAR(10));
END;
GO

-- =============================================
-- Initialize Audit Configuration
-- =============================================

-- Configure audit for main tables
INSERT INTO AuditConfiguration (TableName, SchemaName, IsAuditEnabled, RetentionDays)
VALUES 
    ('Products', 'dbo', 1, 730),      -- 2 years for products
    ('Categories', 'dbo', 1, 730),    -- 2 years for categories
    ('Customers', 'dbo', 1, 2555),    -- 7 years for customers (compliance)
    ('Orders', 'dbo', 1, 2555),       -- 7 years for orders (compliance)
    ('OrderItems', 'dbo', 1, 2555);   -- 7 years for order items (compliance)
GO

PRINT 'Audit trail infrastructure created successfully!';
PRINT 'Tables created: AuditLogDetailed, AuditConfiguration, AuditStatistics, AuditLogArchive';
PRINT 'Views created: vw_AuditActivity, vw_UserAuditSummary, vw_TableAuditSummary';
PRINT 'Procedures created: sp_CleanupAuditLog';
PRINT 'SQL Server features: Computed columns, Columnstore indexes, JSON support, Advanced audit tracking';
GO