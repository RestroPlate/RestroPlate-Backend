-- Update the donatins table constraint to allow 'completed' status
ALTER TABLE dbo.donations DROP CONSTRAINT IF EXISTS CK_donations_status;
GO

ALTER TABLE dbo.donations ADD CONSTRAINT CK_donations_status CHECK (status IN ('available', 'requested', 'collected', 'completed'));
GO
