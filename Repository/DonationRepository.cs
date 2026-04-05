using Microsoft.Data.SqlClient;
using RestroPlate.Models;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    public class DonationRepository : BaseRepository, IDonationRepository
    {
        public DonationRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<int> CreateAsync(Donation donation)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO dbo.donations
                (donation_request_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, claimed_by_center_user_id)
                OUTPUT INSERTED.donation_id
                VALUES
                (@DonationRequestId, @ProviderUserId, @FoodType, @Quantity, @Unit, @ExpirationDate, @PickupAddress, @AvailabilityTime, @Status, @ClaimedByCenterUserId);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", (object?)donation.DonationRequestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProviderUserId", donation.ProviderUserId);
            command.Parameters.AddWithValue("@FoodType", donation.FoodType);
            command.Parameters.AddWithValue("@Quantity", donation.Quantity);
            command.Parameters.AddWithValue("@Unit", donation.Unit);
            command.Parameters.AddWithValue("@ExpirationDate", donation.ExpirationDate);
            command.Parameters.AddWithValue("@PickupAddress", donation.PickupAddress);
            command.Parameters.AddWithValue("@AvailabilityTime", donation.AvailabilityTime);
            command.Parameters.AddWithValue("@Status", donation.Status);
            command.Parameters.AddWithValue("@ClaimedByCenterUserId", (object?)donation.ClaimedByCenterUserId ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return result is int id ? id : Convert.ToInt32(result);
        }

        public async Task<IReadOnlyList<Donation>> GetByUserIdAsync(int providerUserId, string? status = null)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_id, donation_request_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, claimed_by_center_user_id, created_at
                FROM dbo.donations
                WHERE provider_user_id = @ProviderUserId
                  AND (@Status IS NULL OR status = @Status)
                ORDER BY created_at DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProviderUserId", providerUserId);
            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

            var donations = new List<Donation>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donations.Add(MapDonation(reader));
            }

            return donations;
        }

        public async Task<Donation?> GetByIdAsync(int donationId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_id, donation_request_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, claimed_by_center_user_id, created_at
                FROM dbo.donations
                WHERE donation_id = @DonationId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapDonation(reader);
        }

        public async Task<Donation?> GetByIdAsync(int donationId, int providerUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_id, donation_request_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, claimed_by_center_user_id, created_at
                FROM dbo.donations
                WHERE donation_id = @DonationId
                  AND provider_user_id = @ProviderUserId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            command.Parameters.AddWithValue("@ProviderUserId", providerUserId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapDonation(reader);
        }

        public async Task<bool> UpdateAsync(Donation donation)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE dbo.donations
                SET food_type = @FoodType,
                    quantity = @Quantity,
                    unit = @Unit,
                    expiration_date = @ExpirationDate,
                    pickup_address = @PickupAddress,
                    availability_time = @AvailabilityTime
                WHERE donation_id = @DonationId
                  AND provider_user_id = @ProviderUserId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donation.DonationId);
            command.Parameters.AddWithValue("@ProviderUserId", donation.ProviderUserId);
            command.Parameters.AddWithValue("@FoodType", donation.FoodType);
            command.Parameters.AddWithValue("@Quantity", donation.Quantity);
            command.Parameters.AddWithValue("@Unit", donation.Unit);
            command.Parameters.AddWithValue("@ExpirationDate", donation.ExpirationDate);
            command.Parameters.AddWithValue("@PickupAddress", donation.PickupAddress);
            command.Parameters.AddWithValue("@AvailabilityTime", donation.AvailabilityTime);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int donationId, int providerUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                DELETE FROM dbo.donations
                WHERE donation_id = @DonationId
                  AND provider_user_id = @ProviderUserId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            command.Parameters.AddWithValue("@ProviderUserId", providerUserId);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        // new — atomic status-only update used by request/collect transitions
        public async Task<bool> UpdateStatusAsync(int donationId, string newStatus)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE dbo.donations
                SET status = @Status
                WHERE donation_id = @DonationId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            command.Parameters.AddWithValue("@Status", newStatus);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        // new — atomic update of status + claimed_by_center_user_id (used by claim acceptance)
        public async Task<bool> UpdateStatusAndClaimedByAsync(int donationId, string newStatus, int centerUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE dbo.donations
                SET status = @Status,
                    claimed_by_center_user_id = @CenterUserId
                WHERE donation_id = @DonationId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            command.Parameters.AddWithValue("@Status", newStatus);
            command.Parameters.AddWithValue("@CenterUserId", centerUserId);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }

        public async Task<IReadOnlyList<Donation>> GetAvailableAsync(string? location, string? foodType, string? sortBy)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT donation_id, donation_request_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, claimed_by_center_user_id, created_at
                FROM dbo.donations
                WHERE status = 'available'";

            if (!string.IsNullOrWhiteSpace(location))
                sql += " AND pickup_address LIKE @Location";

            if (!string.IsNullOrWhiteSpace(foodType))
                sql += " AND food_type LIKE @FoodType";

            sql += sortBy?.ToLowerInvariant() switch
            {
                "expirationdate" => " ORDER BY expiration_date ASC",
                _ => " ORDER BY created_at DESC"
            };

            sql += ";";

            using var command = new SqlCommand(sql, connection);

            if (!string.IsNullOrWhiteSpace(location))
                command.Parameters.AddWithValue("@Location", $"%{location.Trim()}%");

            if (!string.IsNullOrWhiteSpace(foodType))
                command.Parameters.AddWithValue("@FoodType", $"%{foodType.Trim()}%");

            var donations = new List<Donation>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donations.Add(MapDonation(reader));
            }

            return donations;
        }

        public async Task<IReadOnlyList<Donation>> GetCenterInventoryAsync(int centerUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT d.donation_id, d.donation_request_id, d.provider_user_id, d.food_type, d.quantity, d.unit, d.expiration_date, d.pickup_address, d.availability_time, d.status, d.claimed_by_center_user_id, d.created_at,
                       ISNULL(il.is_published, 0) as is_published,
                       il.inventory_log_id
                FROM dbo.donations d
                LEFT JOIN dbo.inventory_logs il ON d.donation_id = il.donation_id AND il.distribution_center_user_id = @CenterUserId
                WHERE d.claimed_by_center_user_id = @CenterUserId
                  AND d.status IN ('requested', 'collected')
                ORDER BY d.created_at DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CenterUserId", centerUserId);

            var donations = new List<Donation>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donations.Add(MapDonation(reader));
            }

            return donations;
        }

        public async Task<decimal> GetTotalFulfilledQuantityAsync(int donationRequestId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT ISNULL(SUM(quantity), 0)
                FROM dbo.donations
                WHERE donation_request_id = @DonationRequestId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", donationRequestId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToDecimal(result ?? 0);
        }

        private static Donation MapDonation(SqlDataReader reader) => new()
        {
            DonationId = reader.GetInt32(reader.GetOrdinal("donation_id")),
            DonationRequestId = !reader.IsDBNull(reader.GetOrdinal("donation_request_id")) ? reader.GetInt32(reader.GetOrdinal("donation_request_id")) : null,
            ProviderUserId = reader.GetInt32(reader.GetOrdinal("provider_user_id")),
            FoodType = reader.GetString(reader.GetOrdinal("food_type")),
            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
            Unit = reader.GetString(reader.GetOrdinal("unit")),
            ExpirationDate = reader.GetDateTime(reader.GetOrdinal("expiration_date")),
            PickupAddress = reader.GetString(reader.GetOrdinal("pickup_address")),
            AvailabilityTime = reader.GetString(reader.GetOrdinal("availability_time")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            ClaimedByCenterUserId = !reader.IsDBNull(reader.GetOrdinal("claimed_by_center_user_id")) ? reader.GetInt32(reader.GetOrdinal("claimed_by_center_user_id")) : null,
            IsPublished = HasColumn(reader, "is_published") && !reader.IsDBNull(reader.GetOrdinal("is_published")) && reader.GetBoolean(reader.GetOrdinal("is_published")),
            InventoryLogId = HasColumn(reader, "inventory_log_id") && !reader.IsDBNull(reader.GetOrdinal("inventory_log_id")) ? reader.GetInt32(reader.GetOrdinal("inventory_log_id")) : null,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
