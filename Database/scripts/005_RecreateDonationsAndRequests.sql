-- Recreate Donations and Donation Requests tables
-- This script drops the existing tables if they exist and recreates them with the current schema.

-- Drop tables if they exist
IF OBJECT_ID('dbo.donations', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.donations;
END
IF OBJECT_ID('dbo.donation_requests', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.donation_requests;
END
GO

-- Recreate donation_requests table
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
    CONSTRAINT FK_donation_requests_distribution_center FOREIGN KEY (distribution_center_user_id) REFERENCES dbo.users(user_id),
    CONSTRAINT CK_donation_requests_status CHECK (status IN ('pending', 'completed')),
    CONSTRAINT CK_donation_requests_quantity CHECK (requested_quantity > 0)
);
CREATE INDEX IX_donation_requests_distribution_center_user_id ON dbo.donation_requests(distribution_center_user_id);
GO

-- Recreate donations table
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
    CONSTRAINT FK_donations_users_provider FOREIGN KEY (provider_user_id) REFERENCES dbo.users(user_id),
    CONSTRAINT CK_donations_status CHECK (status IN ('available', 'requested', 'collected')),
    CONSTRAINT CK_donations_quantity CHECK (quantity > 0)
);
CREATE INDEX IX_donations_provider_user_id ON dbo.donations(provider_user_id);
GO
