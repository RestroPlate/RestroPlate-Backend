-- Add claimed_by_center_user_id column to donations table
-- Stores the distribution center user ID that claimed the donation

ALTER TABLE dbo.donations
ADD claimed_by_center_user_id INT NULL;
GO

ALTER TABLE dbo.donations
ADD CONSTRAINT FK_donations_claimed_by_center FOREIGN KEY (claimed_by_center_user_id) REFERENCES dbo.users(user_id);
GO
