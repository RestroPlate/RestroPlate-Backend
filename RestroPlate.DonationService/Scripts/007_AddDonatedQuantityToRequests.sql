-- Add donated_quantity column to donation_requests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[donation_requests]') AND name = 'donated_quantity')
BEGIN
    ALTER TABLE [dbo].[donation_requests]
    ADD [donated_quantity] DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO
