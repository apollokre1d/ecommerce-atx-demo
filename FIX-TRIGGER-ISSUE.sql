-- =============================================
-- FIX AUDIT TRIGGER CONSTRAINT ISSUE
-- Update AuditLog constraint to allow additional actions
-- =============================================

USE ECommerceDB;
GO

PRINT 'Fixing AuditLog constraint to allow additional audit actions...';

-- Drop the existing constraint
ALTER TABLE AuditLog DROP CONSTRAINT CK_AuditLog_Action_Valid;
GO

-- Add updated constraint with additional allowed actions
ALTER TABLE AuditLog ADD CONSTRAINT CK_AuditLog_Action_Valid 
    CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE', 'HIGH_VALUE', 'CANCELLED', 'REVIEW', 'EMAIL_CHANGE', 'NAME_CHANGE', 'DEACTIVATED', 'ERROR'));
GO

PRINT 'AuditLog constraint updated successfully!';
PRINT 'Now you can re-run the sample data script.';
GO