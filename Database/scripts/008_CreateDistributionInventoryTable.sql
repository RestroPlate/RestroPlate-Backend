-- Create Distribution Inventory table for tracking collected donations
-- Also update donation_requests status constraint to allow 'collected' status

-- Drop and recreate the status constraint on donation_requests to include 'collected'
ALTER TABLE dbo.donation_requests DROP CONSTRAINT CK_donation_requests_status;
ALTER TABLE dbo.donation_requests ADD CONSTRAINT CK_donation_requests_status CHECK (status IN ('pending', 'completed', 'collected'));
GO

-- Create distribution_inventory table
CREATE TABLE dbo.distribution_inventory
(
    inventory_id INT IDENTITY(1,1) NOT NULL,
    donation_request_id INT NOT NULL,
    collected_quantity DECIMAL(10, 2) NOT NULL,
    collection_date DATETIME2 NOT NULL CONSTRAINT DF_distribution_inventory_collection_date DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_distribution_inventory PRIMARY KEY (inventory_id),
    CONSTRAINT FK_distribution_inventory_donation_requests FOREIGN KEY (donation_request_id) REFERENCES dbo.donation_requests(donation_request_id),
    CONSTRAINT UQ_distribution_inventory_donation_request UNIQUE (donation_request_id),
    CONSTRAINT CK_distribution_inventory_quantity CHECK (collected_quantity > 0)
);
CREATE INDEX IX_distribution_inventory_donation_request_id ON dbo.distribution_inventory(donation_request_id);
GO
