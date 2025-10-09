-- =============================================
-- FIX SQL SERVER AUTHENTICATION FOR ATX SQL
-- Enable Mixed Mode Authentication and Configure User
-- =============================================

-- Step 1: Enable SQL Server and Windows Authentication Mode (Mixed Mode)
-- This requires SQL Server restart, so we'll do it via registry/configuration

PRINT 'Configuring SQL Server for Mixed Mode Authentication...';

-- Enable SQL Server Authentication (Mixed Mode)
EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'LoginMode', 
    REG_DWORD, 
    2; -- 1 = Windows Only, 2 = Mixed Mode

PRINT 'Mixed Mode Authentication enabled. SQL Server restart required.';
PRINT '';

-- Step 2: Enable sa account (if needed for testing)
PRINT 'Enabling sa account...';

ALTER LOGIN sa ENABLE;
ALTER LOGIN sa WITH PASSWORD = 'YourStrongPassword123!';

PRINT 'sa account enabled with new password.';
PRINT '';

-- Step 3: Create and configure sqlatx user properly
PRINT 'Creating sqlatx login and user...';

-- Drop existing login if it exists
IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'sqlatx')
BEGIN
    DROP LOGIN sqlatx;
    PRINT 'Existing sqlatx login dropped.';
END

-- Create new login with strong password
CREATE LOGIN sqlatx WITH PASSWORD = 'ATXSql123!@#', 
    DEFAULT_DATABASE = ECommerceDB,
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = OFF;

PRINT 'sqlatx login created successfully.';

-- Switch to ECommerceDB to create user
USE ECommerceDB;
GO

-- Drop existing user if it exists
IF EXISTS (SELECT name FROM sys.database_principals WHERE name = 'sqlatx')
BEGIN
    DROP USER sqlatx;
    PRINT 'Existing sqlatx user dropped.';
END

-- Create database user
CREATE USER sqlatx FOR LOGIN sqlatx;

-- Grant necessary permissions for ATX SQL
ALTER ROLE db_datareader ADD MEMBER sqlatx;
ALTER ROLE db_datawriter ADD MEMBER sqlatx;
ALTER ROLE db_ddladmin ADD MEMBER sqlatx;

-- Grant additional permissions needed for schema analysis
GRANT VIEW DEFINITION TO sqlatx;
GRANT VIEW ANY DEFINITION TO sqlatx;
GRANT EXECUTE TO sqlatx;

-- Grant permissions on specific schema objects
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO sqlatx;

PRINT 'sqlatx user created and permissions granted.';
PRINT '';

-- Step 4: Test the connection
PRINT 'Testing sqlatx login...';

-- This will show current user context
SELECT 
    SYSTEM_USER as SystemUser,
    USER_NAME() as DatabaseUser,
    DB_NAME() as CurrentDatabase;

PRINT '';
PRINT '=============================================';
PRINT 'SQL SERVER AUTHENTICATION CONFIGURATION COMPLETE';
PRINT '=============================================';
PRINT '';
PRINT 'IMPORTANT: SQL Server restart is required for Mixed Mode to take effect!';
PRINT '';
PRINT 'Connection Details for ATX SQL:';
PRINT 'Server: 3.67.133.184,1433';
PRINT 'Database: ECommerceDB';
PRINT 'Username: sqlatx';
PRINT 'Password: ATXSql123!@#';
PRINT '';
PRINT 'Alternative (if sa is preferred):';
PRINT 'Username: sa';
PRINT 'Password: YourStrongPassword123!';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Restart SQL Server service';
PRINT '2. Test connection from bastion host';
PRINT '3. Configure ATX SQL with these credentials';

GO