using Microsoft.Data.SqlClient;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    // new — persists every collect action by a DC into inventory_logs
    public class InventoryLogRepository : BaseRepository, IInventoryLogRepository
    {
        public InventoryLogRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<InventoryLogResponseDto> CreateAsync(InventoryLog inventoryLog)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO dbo.inventory_logs
                (donation_id, donation_request_id, distribution_center_user_id, collected_amount, is_published)
                OUTPUT INSERTED.inventory_log_id, INSERTED.collected_at
                VALUES
                (@DonationId, @DonationRequestId, @DistributionCenterUserId, @CollectedAmount, @IsPublished);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", inventoryLog.DonationId);
            command.Parameters.AddWithValue("@DonationRequestId", (object?)inventoryLog.DonationRequestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DistributionCenterUserId", inventoryLog.DistributionCenterUserId);
            command.Parameters.AddWithValue("@CollectedAmount", inventoryLog.CollectedAmount);
            command.Parameters.AddWithValue("@IsPublished", inventoryLog.IsPublished);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to insert inventory log entry.");

            inventoryLog.InventoryLogId = reader.GetInt32(0);
            inventoryLog.CollectedAt = reader.GetDateTime(1);

            return new InventoryLogResponseDto
            {
                InventoryLogId = inventoryLog.InventoryLogId,
                DonationId = inventoryLog.DonationId,
                DonationRequestId = inventoryLog.DonationRequestId,
                DistributionCenterUserId = inventoryLog.DistributionCenterUserId,
                CollectedAmount = inventoryLog.CollectedAmount,
                DistributedQuantity = 0,
                IsPublished = inventoryLog.IsPublished,
                CollectedAt = inventoryLog.CollectedAt
            };
        }

        public async Task UpdateIsPublishedAsync(int inventoryLogId, bool isPublished)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = "UPDATE dbo.inventory_logs SET is_published = @IsPublished WHERE inventory_log_id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IsPublished", isPublished);
            command.Parameters.AddWithValue("@Id", inventoryLogId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync()
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT inventory_log_id, donation_id, donation_request_id, distribution_center_user_id, collected_amount, distributed_quantity, is_published, collected_at
                FROM dbo.inventory_logs
                WHERE is_published = 1
                ORDER BY collected_at DESC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var logs = new List<InventoryLogResponseDto>();
            while (await reader.ReadAsync())
            {
                logs.Add(new InventoryLogResponseDto
                {
                    InventoryLogId = reader.GetInt32(0),
                    DonationId = reader.GetInt32(1),
                    DonationRequestId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    DistributionCenterUserId = reader.GetInt32(3),
                    CollectedAmount = reader.GetDecimal(4),
                    DistributedQuantity = reader.GetDecimal(5),
                    IsPublished = reader.GetBoolean(6),
                    CollectedAt = reader.GetDateTime(7)
                });
            }
            return logs;
        }

        public async Task<InventoryLog?> GetByIdAsync(int inventoryLogId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT inventory_log_id, donation_id, donation_request_id, distribution_center_user_id, collected_amount, distributed_quantity, is_published, collected_at
                FROM dbo.inventory_logs
                WHERE inventory_log_id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", inventoryLogId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new InventoryLog
            {
                InventoryLogId = reader.GetInt32(0),
                DonationId = reader.GetInt32(1),
                DonationRequestId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                DistributionCenterUserId = reader.GetInt32(3),
                CollectedAmount = reader.GetDecimal(4),
                DistributedQuantity = reader.GetDecimal(5),
                IsPublished = reader.GetBoolean(6),
                CollectedAt = reader.GetDateTime(7)
            };
        }

        public async Task UpdateDistributedQuantityAsync(int inventoryLogId, decimal addedQuantity)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE dbo.inventory_logs 
                SET distributed_quantity = distributed_quantity + @AddedQuantity 
                WHERE inventory_log_id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AddedQuantity", addedQuantity);
            command.Parameters.AddWithValue("@Id", inventoryLogId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<CenterWithDonationsDto>> GetCentersWithPublishedDonationsAsync()
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    u.user_id, u.name, u.address, u.phone_number,
                    il.inventory_log_id, il.collected_amount, il.distributed_quantity, il.collected_at,
                    d.food_type, d.expiration_date, d.donation_id, d.unit
                FROM dbo.users u
                LEFT JOIN dbo.inventory_logs il ON u.user_id = il.distribution_center_user_id AND il.is_published = 1
                LEFT JOIN dbo.donations d ON il.donation_id = d.donation_id
                WHERE u.user_type = 'DISTRIBUTION_CENTER'
                ORDER BY u.name, il.collected_at DESC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var centersMap = new Dictionary<int, CenterWithDonationsDto>();

            while (await reader.ReadAsync())
            {
                int userId = reader.GetInt32(0);
                if (!centersMap.TryGetValue(userId, out var center))
                {
                    center = new CenterWithDonationsDto
                    {
                        CenterId = userId,
                        Name = reader.GetString(1),
                        Address = reader.GetString(2),
                        PhoneNumber = reader.GetString(3),
                        PublishedDonations = new List<PublishedDonationDto>()
                    };
                    centersMap.Add(userId, center);
                }

                if (!reader.IsDBNull(4)) // inventory_log_id
                {
                    var collected = reader.GetDecimal(5);     // collected_amount
                    var distributed = reader.GetDecimal(6);   // distributed_quantity
                    var remaining = collected - distributed;

                    if (remaining > 0)  // skip fully distributed items
                    {
                        center.PublishedDonations.Add(new PublishedDonationDto
                        {
                            DonationId = reader.GetInt32(10),
                            FoodType = reader.GetString(8),
                            Quantity = remaining,
                            Unit = reader.GetString(11),
                            ExpirationDate = reader.GetDateTime(9),
                            CollectedAt = reader.GetDateTime(7)
                        });
                    }
                }
            }

            return centersMap.Values;
        }
    }
}
