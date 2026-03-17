IF OBJECT_ID('dbo.donation_requests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.donation_requests
    (
        donation_request_id INT IDENTITY(1,1) NOT NULL,
        donation_id INT NOT NULL,
        distribution_center_user_id INT NOT NULL,
        requested_quantity DECIMAL(10, 2) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_donation_requests_status DEFAULT 'pending',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_donation_requests_created_at DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_donation_requests PRIMARY KEY (donation_request_id),
        CONSTRAINT FK_donation_requests_donations FOREIGN KEY (donation_id) REFERENCES dbo.donations(donation_id),
        CONSTRAINT FK_donation_requests_distribution_center FOREIGN KEY (distribution_center_user_id) REFERENCES dbo.users(user_id),
        CONSTRAINT CK_donation_requests_status CHECK (status IN ('pending', 'approved', 'rejected')),
        CONSTRAINT CK_donation_requests_quantity CHECK (requested_quantity > 0)
    );

    CREATE INDEX IX_donation_requests_donation_id ON dbo.donation_requests(donation_id);
    CREATE INDEX IX_donation_requests_distribution_center_user_id ON dbo.donation_requests(distribution_center_user_id);
END
GO
