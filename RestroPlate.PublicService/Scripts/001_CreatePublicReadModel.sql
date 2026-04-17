CREATE TABLE PublishedDonationsView
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DonationId INT NOT NULL,
    CenterId INT NOT NULL,
    CenterName NVARCHAR(255),
    CenterAddress NVARCHAR(MAX),
    FoodType NVARCHAR(100),
    Quantity FLOAT,
    Unit NVARCHAR(50),
    CollectedAt DATETIME
);