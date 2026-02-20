-- Drop table if exists
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL
    DROP TABLE dbo.users;
GO

-- Create table
CREATE TABLE dbo.users
(
  user_id INT IDENTITY(1,1) NOT NULL,

  email NVARCHAR(255) NOT NULL,
  password_hash NVARCHAR(255) NOT NULL,
  phone_number NVARCHAR(20) NULL,

  user_type NVARCHAR(50) NOT NULL
    CHECK (user_type IN ('DONOR', 'DISTRIBUTION_CENTER')),

  address NVARCHAR(MAX) NULL,

  created_at DATETIME NOT NULL DEFAULT GETDATE(),

  CONSTRAINT PK_users PRIMARY KEY (user_id),
  CONSTRAINT UQ_users_email UNIQUE (email)
);
GO