using Microsoft.Data.SqlClient;
using RestroPlate.Models;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    public class DonationClaimRepository : BaseRepository, IDonationClaimRepository
    {
        public DonationClaimRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<int> CreateAsync(DonationClaim claim)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO dbo.donation_claims (donation_id, center_user_id, donator_user_id, status)
                VALUES (@DonationId, @CenterUserId, @DonatorUserId, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            command.Parameters.AddWithValue("@DonationId", claim.DonationId);
            command.Parameters.AddWithValue("@CenterUserId", claim.CenterUserId);
            command.Parameters.AddWithValue("@DonatorUserId", claim.DonatorUserId);
            command.Parameters.AddWithValue("@Status", claim.Status);

            connection.Open();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<DonationClaim?> GetByIdAsync(int claimId)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.donation_claims WHERE claim_id = @ClaimId";
            command.Parameters.AddWithValue("@ClaimId", claimId);

            connection.Open();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapClaim(reader);
            }
            return null;
        }

        public async Task<IReadOnlyList<DonationClaim>> GetByDonationIdAsync(int donationId)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.donation_claims WHERE donation_id = @DonationId ORDER BY created_at DESC";
            command.Parameters.AddWithValue("@DonationId", donationId);

            connection.Open();
            using var reader = await command.ExecuteReaderAsync();
            var claims = new List<DonationClaim>();
            while (await reader.ReadAsync())
            {
                claims.Add(MapClaim(reader));
            }
            return claims;
        }

        public async Task<IReadOnlyList<DonationClaim>> GetByDonatorUserIdAsync(int donatorUserId)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.donation_claims WHERE donator_user_id = @DonatorUserId ORDER BY created_at DESC";
            command.Parameters.AddWithValue("@DonatorUserId", donatorUserId);

            connection.Open();
            using var reader = await command.ExecuteReaderAsync();
            var claims = new List<DonationClaim>();
            while (await reader.ReadAsync())
            {
                claims.Add(MapClaim(reader));
            }
            return claims;
        }

        public async Task<IReadOnlyList<DonationClaim>> GetByCenterUserIdAsync(int centerUserId)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.donation_claims WHERE center_user_id = @CenterUserId ORDER BY created_at DESC";
            command.Parameters.AddWithValue("@CenterUserId", centerUserId);

            connection.Open();
            using var reader = await command.ExecuteReaderAsync();
            var claims = new List<DonationClaim>();
            while (await reader.ReadAsync())
            {
                claims.Add(MapClaim(reader));
            }
            return claims;
        }

        public async Task<bool> UpdateStatusAsync(int claimId, string newStatus)
        {
            using var connection = CreateConnection();
            using var command = (SqlCommand)connection.CreateCommand();
            command.CommandText = "UPDATE dbo.donation_claims SET status = @Status WHERE claim_id = @ClaimId";
            command.Parameters.AddWithValue("@Status", newStatus);
            command.Parameters.AddWithValue("@ClaimId", claimId);

            connection.Open();
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static DonationClaim MapClaim(SqlDataReader reader)
        {
            return new DonationClaim
            {
                ClaimId = reader.GetInt32(reader.GetOrdinal("claim_id")),
                DonationId = reader.GetInt32(reader.GetOrdinal("donation_id")),
                CenterUserId = reader.GetInt32(reader.GetOrdinal("center_user_id")),
                DonatorUserId = reader.GetInt32(reader.GetOrdinal("donator_user_id")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }
}
