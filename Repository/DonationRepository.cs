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
                (provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status)
                OUTPUT INSERTED.donation_id
                VALUES
                (@ProviderUserId, @FoodType, @Quantity, @Unit, @ExpirationDate, @PickupAddress, @AvailabilityTime, @Status);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProviderUserId", donation.ProviderUserId);
            command.Parameters.AddWithValue("@FoodType", donation.FoodType);
            command.Parameters.AddWithValue("@Quantity", donation.Quantity);
            command.Parameters.AddWithValue("@Unit", donation.Unit);
            command.Parameters.AddWithValue("@ExpirationDate", donation.ExpirationDate);
            command.Parameters.AddWithValue("@PickupAddress", donation.PickupAddress);
            command.Parameters.AddWithValue("@AvailabilityTime", donation.AvailabilityTime);
            command.Parameters.AddWithValue("@Status", donation.Status);

            var result = await command.ExecuteScalarAsync();
            return result is int id ? id : Convert.ToInt32(result);
        }

        public async Task<IReadOnlyList<Donation>> GetByUserIdAsync(int providerUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT donation_id, provider_user_id, food_type, quantity, unit, expiration_date, pickup_address, availability_time, status, created_at
                FROM dbo.donations
                WHERE provider_user_id = @ProviderUserId
                ORDER BY created_at DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProviderUserId", providerUserId);

            var donations = new List<Donation>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                donations.Add(MapDonation(reader));
            }

            return donations;
        }

        private static Donation MapDonation(SqlDataReader reader) => new()
        {
            DonationId = reader.GetInt32(reader.GetOrdinal("donation_id")),
            ProviderUserId = reader.GetInt32(reader.GetOrdinal("provider_user_id")),
            FoodType = reader.GetString(reader.GetOrdinal("food_type")),
            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
            Unit = reader.GetString(reader.GetOrdinal("unit")),
            ExpirationDate = reader.GetDateTime(reader.GetOrdinal("expiration_date")),
            PickupAddress = reader.GetString(reader.GetOrdinal("pickup_address")),
            AvailabilityTime = reader.GetString(reader.GetOrdinal("availability_time")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
