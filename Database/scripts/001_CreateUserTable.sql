-- Switch to or create the database
IF NOT EXISTS (SELECT name
FROM master.sys.databases
WHERE name = 'restroplate')
BEGIN
  CREATE DATABASE restroplate;
END
GO

-- Use the database
USE restroplate;
GO

-- Drop table if exists
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL
    DROP TABLE dbo.users;
GO

-- Create table
CREATE TABLE dbo.users
(
  user_id INT IDENTITY(1,1) NOT NULL,
  -- AUTO_INCREMENT equivalent
  email NVARCHAR(255) NOT NULL,
  password_hash NVARCHAR(255) NOT NULL,
  phone_number NVARCHAR(20) NULL,

  -- ENUM replacement using CHECK constraint
  user_type NVARCHAR(50) NOT NULL
    CHECK (user_type IN ('DONOR', 'DISTRIBUTION_CENTER')),

  -- JSON stored as NVARCHAR(MAX) in SQL Server
  address NVARCHAR(MAX) NULL,

  -- TIMESTAMP default CURRENT_TIMESTAMP
  created_at DATETIME NOT NULL DEFAULT GETDATE(),

  CONSTRAINT PK_users PRIMARY KEY (user_id),
  CONSTRAINT UQ_users_email UNIQUE (email)
);
GO

-- Table is empty in your dump, so no data insertion needed
-- If there were data, we would convert MySQL INSERT statements to T-SQL INSERT statements
