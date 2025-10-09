-- =============================================
-- Create Application User Account
-- Run this as Administrator or SA
-- =============================================

USE master;
GO

-- Create login if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'ecommerce_app')
BEGIN
    CREATE LOGIN [ecommerce_app] WITH PASSWORD = 'AppPassword123!';
    PRINT 'Login ecommerce_app created successfully.';
END
ELSE
BEGIN
    PRINT 'Login ecommerce_app already exists.';
END
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    CREATE DATABASE ECommerceDB;
    PRINT 'Database ECommerceDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database ECommerceDB already exists.';
END
GO

-- Switch to ECommerceDB and create user
USE ECommerceDB;
GO

-- Create user if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'ecommerce_app')
BEGIN
    CREATE USER [ecommerce_app] FOR LOGIN [ecommerce_app];
    ALTER ROLE db_owner ADD MEMBER [ecommerce_app];
    PRINT 'User ecommerce_app created and added to db_owner role.';
END
ELSE
BEGIN
    PRINT 'User ecommerce_app already exists.';
END
GO

-- Test the connection
SELECT 
    'Connection successful!' AS Status,
    @@VERSION AS SQLServerVersion,
    DB_NAME() AS CurrentDatabase,
    SUSER_SNAME() AS CurrentUser,
    GETUTCDATE() AS CurrentTime;
GO

PRINT 'User account setup completed successfully!';
PRINT 'You can now connect with:';
PRINT 'Username: ecommerce_app';
PRINT 'Password: AppPassword123!';
GO