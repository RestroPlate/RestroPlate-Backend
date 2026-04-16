IF OBJECT_ID('dbo.donations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.donations
    (
        donation_id INT IDENTITY(1,1) NOT NULL,
        donation_request_id INT NULL,
        provider_user_id INT NOT NULL,
        food_type NVARCHAR(225) NOT NULL,
        quantity DECIMAL(10, 2) NOT NULL,
        unit NVARCHAR(50) NOT NULL,
        expiration_date DATETIME2 NOT NULL,
        pickup_address NVARCHAR(500) NOT NULL,
        availability_time NVARCHAR(120) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_donations_status DEFAULT 'available',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_donations_created_at DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_donations PRIMARY KEY (donation_id),
        CONSTRAINT FK_donations_donation_requests FOREIGN KEY (donation_request_id) REFERENCES dbo.donation_requests(donation_request_id),
        CONSTRAINT CK_donations_status CHECK (status IN ('available', 'requested', 'collected')),
        CONSTRAINT CK_donations_quantity CHECK (quantity > 0)
    );

    CREATE INDEX IX_donations_provider_user_id ON dbo.donations(provider_user_id);
END
GO
