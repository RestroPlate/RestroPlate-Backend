using Microsoft.Data.SqlClient;
using RestroPlate.Models;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    public class DonationRequestRepository : BaseRepository, IDonationRequestRepository
    {
        public DonationRequestRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<DonationRequest> CreateAsync(DonationRequest donationRequest)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();
            using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                const string updateDonationSql = @"
                    UPDATE dbo.donations
                    SET status = 'requested'
                    WHERE donation_id = @DonationId
                      AND status = 'available';";

                using (var updateDonationCommand = new SqlCommand(updateDonationSql, connection, transaction))
                {
                    updateDonationCommand.Parameters.AddWithValue("@DonationId", donationRequest.DonationId);

                    var affectedRows = await updateDonationCommand.ExecuteNonQueryAsync();
                    if (affectedRows == 0)
                        throw new InvalidOperationException("Donation is no longer available.");
                }

                const string insertSql = @"
                    INSERT INTO dbo.donation_requests
                    (donation_id, distribution_center_user_id, requested_quantity, status)
                    OUTPUT INSERTED.donation_request_id, INSERTED.created_at
                    VALUES
                    (@DonationId, @DistributionCenterUserId, @RequestedQuantity, @Status);";

                using var insertCommand = new SqlCommand(insertSql, connection, transaction);
                insertCommand.Parameters.AddWithValue("@DonationId", donationRequest.DonationId);
                insertCommand.Parameters.AddWithValue("@DistributionCenterUserId", donationRequest.DistributionCenterUserId);
                insertCommand.Parameters.AddWithValue("@RequestedQuantity", donationRequest.RequestedQuantity);
                insertCommand.Parameters.AddWithValue("@Status", donationRequest.Status);

                using var reader = await insertCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Failed to create donation request.");

                donationRequest.DonationRequestId = reader.GetInt32(0);
                donationRequest.CreatedAt = reader.GetDateTime(1);

                await reader.CloseAsync();
                await transaction.CommitAsync();

                return donationRequest;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IReadOnlyList<DonationRequest>> GetByProviderUserIdAsync(int providerUserId, string? status = null)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT dr.donation_request_id, dr.donation_id, d.provider_user_id, dr.distribution_center_user_id,
                       dr.requested_quantity, dr.status, dr.created_at, d.food_type, d.unit
                FROM dbo.donation_requests dr
                INNER JOIN dbo.donations d ON d.donation_id = dr.donation_id
                WHERE d.provider_user_id = @ProviderUserId
                  AND (@Status IS NULL OR dr.status = @Status)
                ORDER BY dr.created_at DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProviderUserId", providerUserId);
            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

            var donationRequests = new List<DonationRequest>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donationRequests.Add(MapDonationRequest(reader));
            }

            return donationRequests;
        }

        public async Task<IReadOnlyList<DonationRequest>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId, string? status = null)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT dr.donation_request_id, dr.donation_id, d.provider_user_id, dr.distribution_center_user_id,
                       dr.requested_quantity, dr.status, dr.created_at, d.food_type, d.unit
                FROM dbo.donation_requests dr
                INNER JOIN dbo.donations d ON d.donation_id = dr.donation_id
                WHERE dr.distribution_center_user_id = @DistributionCenterUserId
                  AND (@Status IS NULL OR dr.status = @Status)
                ORDER BY dr.created_at DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DistributionCenterUserId", distributionCenterUserId);
            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

            var donationRequests = new List<DonationRequest>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donationRequests.Add(MapDonationRequest(reader));
            }

            return donationRequests;
        }

        private static DonationRequest MapDonationRequest(SqlDataReader reader) => new()
        {
            DonationRequestId = reader.GetInt32(reader.GetOrdinal("donation_request_id")),
            DonationId = reader.GetInt32(reader.GetOrdinal("donation_id")),
            ProviderUserId = reader.GetInt32(reader.GetOrdinal("provider_user_id")),
            DistributionCenterUserId = reader.GetInt32(reader.GetOrdinal("distribution_center_user_id")),
            RequestedQuantity = reader.GetDecimal(reader.GetOrdinal("requested_quantity")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            FoodType = reader.GetString(reader.GetOrdinal("food_type")),
            Unit = reader.GetString(reader.GetOrdinal("unit"))
        };
    }
}
