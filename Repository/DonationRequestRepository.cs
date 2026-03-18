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
            
            const string sql = @"
                INSERT INTO dbo.donation_requests
                (distribution_center_user_id, food_type, requested_quantity, unit, status)
                OUTPUT INSERTED.donation_request_id, INSERTED.created_at
                VALUES
                (@DistributionCenterUserId, @FoodType, @RequestedQuantity, @Unit, @Status);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DistributionCenterUserId", donationRequest.DistributionCenterUserId);
            command.Parameters.AddWithValue("@FoodType", donationRequest.FoodType);
            command.Parameters.AddWithValue("@RequestedQuantity", donationRequest.RequestedQuantity);
            command.Parameters.AddWithValue("@Unit", donationRequest.Unit);
            command.Parameters.AddWithValue("@Status", donationRequest.Status);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to create donation request.");

            donationRequest.DonationRequestId = reader.GetInt32(0);
            donationRequest.CreatedAt = reader.GetDateTime(1);

            return donationRequest;
        }

        public async Task<IReadOnlyList<DonationRequest>> GetAvailableAsync(string? status = null)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_request_id, distribution_center_user_id, requested_quantity, status, created_at, food_type, unit
                FROM dbo.donation_requests
                WHERE status = 'pending'
                  AND (@Status IS NULL OR status = @Status)
                ORDER BY created_at DESC;";

            using var command = new SqlCommand(sql, connection);
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
                SELECT donation_request_id, distribution_center_user_id, requested_quantity, status, created_at, food_type, unit
                FROM dbo.donation_requests
                WHERE distribution_center_user_id = @DistributionCenterUserId
                  AND (@Status IS NULL OR status = @Status)
                ORDER BY created_at DESC;";

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

        public async Task<DonationRequest?> GetByIdAsync(int donationRequestId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_request_id, distribution_center_user_id, requested_quantity, status, created_at, food_type, unit
                FROM dbo.donation_requests
                WHERE donation_request_id = @DonationRequestId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", donationRequestId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapDonationRequest(reader);
        }

        public async Task<bool> UpdateAsync(DonationRequest donationRequest)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE dbo.donation_requests
                SET food_type = @FoodType,
                    requested_quantity = @RequestedQuantity,
                    unit = @Unit,
                    status = @Status
                WHERE donation_request_id = @DonationRequestId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", donationRequest.DonationRequestId);
            command.Parameters.AddWithValue("@FoodType", donationRequest.FoodType);
            command.Parameters.AddWithValue("@RequestedQuantity", donationRequest.RequestedQuantity);
            command.Parameters.AddWithValue("@Unit", donationRequest.Unit);
            command.Parameters.AddWithValue("@Status", donationRequest.Status);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        private static DonationRequest MapDonationRequest(SqlDataReader reader) => new()
        {
            DonationRequestId = reader.GetInt32(reader.GetOrdinal("donation_request_id")),
            DistributionCenterUserId = reader.GetInt32(reader.GetOrdinal("distribution_center_user_id")),
            RequestedQuantity = reader.GetDecimal(reader.GetOrdinal("requested_quantity")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            FoodType = reader.GetString(reader.GetOrdinal("food_type")),
            Unit = reader.GetString(reader.GetOrdinal("unit"))
        };
    }
}
