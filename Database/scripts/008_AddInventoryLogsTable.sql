-- new: InventoryLogs table — tracks every collect action performed by a Distribution Center
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'inventory_logs' AND type = 'U'
)
BEGIN
    CREATE TABLE dbo.inventory_logs (
        inventory_log_id         INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        donation_id              INT                 NOT NULL REFERENCES dbo.donations(donation_id),
        donation_request_id      INT                 NULL     REFERENCES dbo.donation_requests(donation_request_id),
        distribution_center_user_id INT             NOT NULL REFERENCES dbo.users(user_id),
        collected_amount         DECIMAL(10,2)       NOT NULL,
        collected_at             DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
