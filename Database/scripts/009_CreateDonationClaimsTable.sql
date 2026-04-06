-- Create donation_claims table
-- Allows distribution centers to claim available donations; donors accept or reject.

CREATE TABLE dbo.donation_claims
(
    claim_id          INT IDENTITY(1,1) NOT NULL,
    donation_id       INT NOT NULL,
    center_user_id    INT NOT NULL,
    donator_user_id   INT NOT NULL,
    status            NVARCHAR(20) NOT NULL CONSTRAINT DF_donation_claims_status DEFAULT 'pending',
    created_at        DATETIME2 NOT NULL CONSTRAINT DF_donation_claims_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_donation_claims PRIMARY KEY (claim_id),
    CONSTRAINT FK_donation_claims_donation  FOREIGN KEY (donation_id)     REFERENCES dbo.donations(donation_id),
    CONSTRAINT FK_donation_claims_center    FOREIGN KEY (center_user_id)  REFERENCES dbo.users(user_id),
    CONSTRAINT FK_donation_claims_donator   FOREIGN KEY (donator_user_id) REFERENCES dbo.users(user_id),
    CONSTRAINT CK_donation_claims_status    CHECK (status IN ('pending', 'accepted', 'rejected'))
);
CREATE INDEX IX_donation_claims_donation_id ON dbo.donation_claims(donation_id);
CREATE INDEX IX_donation_claims_donator_user_id ON dbo.donation_claims(donator_user_id);
GO
