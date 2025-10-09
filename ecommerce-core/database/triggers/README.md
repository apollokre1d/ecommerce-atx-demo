# Database Triggers

This directory contains SQL Server triggers for audit trail implementation and business rule enforcement.

## Files

- `tr_Products_Audit.sql` - Product table audit trigger
- `tr_Orders_StatusUpdate.sql` - Order status change trigger
- `tr_Inventory_Update.sql` - Inventory update trigger

## SQL Server Trigger Features

- AFTER INSERT, UPDATE, DELETE triggers
- INSERTED and DELETED pseudo-tables
- SQL Server system functions (SUSER_SNAME(), GETUTCDATE())
- JSON operations for audit logging
- Complex business logic implementation