IF OBJECT_ID('dbo.donation_images', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.donation_images
    (
        image_id       INT IDENTITY(1,1) NOT NULL,
        donation_id    INT NOT NULL,
        image_url      NVARCHAR(500) NOT NULL,
        file_name      NVARCHAR(255) NOT NULL,
        uploaded_at    DATETIME2 NOT NULL CONSTRAINT DF_donation_images_uploaded_at DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_donation_images PRIMARY KEY (image_id),
        CONSTRAINT FK_donation_images_donations FOREIGN KEY (donation_id)
            REFERENCES dbo.donations(donation_id) ON DELETE CASCADE
    );

    CREATE INDEX IX_donation_images_donation_id ON dbo.donation_images(donation_id);
END
GO
