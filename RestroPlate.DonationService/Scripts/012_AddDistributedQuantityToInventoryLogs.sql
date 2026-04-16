-- new: Add distributed_quantity column to inventory_logs table
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.inventory_logs') 
      AND name = 'distributed_quantity'
)
BEGIN
    ALTER TABLE dbo.inventory_logs
    ADD distributed_quantity DECIMAL(10,2) NOT NULL DEFAULT 0;
END
GO
