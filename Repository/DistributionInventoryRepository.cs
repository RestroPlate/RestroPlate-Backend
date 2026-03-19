using Microsoft.Data.SqlClient;
using RestroPlate.Models;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    public class DistributionInventoryRepository : BaseRepository, IDistributionInventoryRepository
    {
        public DistributionInventoryRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<DistributionInventory> UpsertAsync(DistributionInventory inventory)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                MERGE dbo.distribution_inventory AS target
                USING (SELECT @DonationRequestId AS donation_request_id) AS source
                ON target.donation_request_id = source.donation_request_id
                WHEN MATCHED THEN
                    UPDATE SET collected_quantity = @CollectedQuantity,
                               collection_date = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (donation_request_id, collected_quantity, collection_date)
                    VALUES (@DonationRequestId, @CollectedQuantity, SYSUTCDATETIME())
                OUTPUT INSERTED.inventory_id, INSERTED.donation_request_id,
                       INSERTED.collected_quantity, INSERTED.collection_date;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", inventory.DonationRequestId);
            command.Parameters.AddWithValue("@CollectedQuantity", inventory.CollectedQuantity);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to upsert distribution inventory.");

            return MapInventory(reader);
        }

        public async Task<DistributionInventory?> GetByDonationRequestIdAsync(int donationRequestId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT inventory_id, donation_request_id, collected_quantity, collection_date
                FROM dbo.distribution_inventory
                WHERE donation_request_id = @DonationRequestId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationRequestId", donationRequestId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapInventory(reader);
        }

        public async Task<IReadOnlyList<DistributionInventory>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT di.inventory_id, di.donation_request_id, di.collected_quantity, di.collection_date
                FROM dbo.distribution_inventory di
                INNER JOIN dbo.donation_requests dr ON dr.donation_request_id = di.donation_request_id
                WHERE dr.distribution_center_user_id = @DistributionCenterUserId
                ORDER BY di.collection_date DESC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DistributionCenterUserId", distributionCenterUserId);

            var inventoryList = new List<DistributionInventory>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                inventoryList.Add(MapInventory(reader));
            }

            return inventoryList;
        }

        private static DistributionInventory MapInventory(SqlDataReader reader) => new()
        {
            InventoryId = reader.GetInt32(reader.GetOrdinal("inventory_id")),
            DonationRequestId = reader.GetInt32(reader.GetOrdinal("donation_request_id")),
            CollectedQuantity = reader.GetDecimal(reader.GetOrdinal("collected_quantity")),
            CollectionDate = reader.GetDateTime(reader.GetOrdinal("collection_date"))
        };
    }
}
