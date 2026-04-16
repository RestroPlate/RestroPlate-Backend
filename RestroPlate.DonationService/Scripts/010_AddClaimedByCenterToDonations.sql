-- Add claimed_by_center_user_id column to donations table
-- Stores the distribution center user ID that claimed the donation

ALTER TABLE dbo.donations
ADD claimed_by_center_user_id INT NULL;
GO
