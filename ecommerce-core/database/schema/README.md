# Database Schema Scripts

This directory contains SQL Server table creation scripts with SQL Server-specific features for ATX SQL testing.

## Files

- `01-create-tables.sql` - Core table definitions with identity columns, computed columns, and constraints
- `02-create-indexes.sql` - Performance indexes and unique constraints
- `03-create-audit-table.sql` - Audit log table for trigger testing

## SQL Server Features Included

- Identity columns (IDENTITY(1,1))
- Computed columns (PERSISTED)
- SQL Server data types (DATETIME2, NVARCHAR(MAX), DECIMAL(18,2))
- Foreign key constraints
- Check constraints
- Unique indexes