IF OBJECT_ID('dbo.donation_requests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.donation_requests
    (
        donation_request_id INT IDENTITY(1,1) NOT NULL,
        distribution_center_user_id INT NOT NULL,
        food_type NVARCHAR(225) NOT NULL,
        requested_quantity DECIMAL(10, 2) NOT NULL,
        unit NVARCHAR(50) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_donation_requests_status DEFAULT 'pending',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_donation_requests_created_at DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_donation_requests PRIMARY KEY (donation_request_id),
        CONSTRAINT CK_donation_requests_status CHECK (status IN ('pending', 'completed')),
        CONSTRAINT CK_donation_requests_quantity CHECK (requested_quantity > 0)
    );

    CREATE INDEX IX_donation_requests_distribution_center_user_id ON dbo.donation_requests(distribution_center_user_id);
END
GO
