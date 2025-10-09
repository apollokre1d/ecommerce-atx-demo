# Stored Procedures

This directory contains T-SQL stored procedures with SQL Server-specific syntax for ATX SQL transformation testing.

## Files

- `GetProductsByCategory.sql` - Product search with pagination using OFFSET/FETCH
- `ProcessOrder.sql` - Order processing with JSON parameters and transactions
- `GetCustomerOrderHistory.sql` - Customer order history with complex joins

## T-SQL Features Tested

- OFFSET/FETCH pagination
- JSON operations (OPENJSON, FOR JSON)
- Transaction management (BEGIN TRANSACTION, COMMIT, ROLLBACK)
- Error handling (TRY/CATCH, RAISERROR)
- SQL Server functions (GETUTCDATE(), SUSER_SNAME(), SCOPE_IDENTITY())
- CASE statements and conditional logic