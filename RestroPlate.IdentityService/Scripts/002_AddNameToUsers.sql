-- Add name column to existing users table
IF NOT EXISTS (
    SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'users' AND COLUMN_NAME = 'name'
)
BEGIN
    ALTER TABLE dbo.users
    ADD name NVARCHAR(255) NOT NULL DEFAULT '';
END
GO