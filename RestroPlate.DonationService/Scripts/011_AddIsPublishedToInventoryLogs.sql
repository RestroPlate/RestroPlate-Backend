-- Add is_published column to inventory_logs
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.inventory_logs') 
      AND name = 'is_published'
)
BEGIN
    ALTER TABLE dbo.inventory_logs
    ADD is_published BIT NOT NULL DEFAULT 0;
END
GO
