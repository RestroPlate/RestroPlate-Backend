using Microsoft.Data.SqlClient;
using RestroPlate.InventoryService.DTOs;
using RestroPlate.InventoryService.Models;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Repository
{
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
                (donation_id, donation_request_id, distribution_center_user_id, collected_amount, distributed_quantity, is_published)
                OUTPUT INSERTED.inventory_log_id, INSERTED.collected_at
                VALUES
                (@DonationId, @DonationRequestId, @DistributionCenterUserId, @CollectedAmount, @DistributedQuantity, @IsPublished);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", inventoryLog.DonationId);
            command.Parameters.AddWithValue("@DonationRequestId", (object?)inventoryLog.DonationRequestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DistributionCenterUserId", inventoryLog.DistributionCenterUserId);
            command.Parameters.AddWithValue("@CollectedAmount", inventoryLog.CollectedAmount);
            command.Parameters.AddWithValue("@DistributedQuantity", inventoryLog.DistributedQuantity);
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
                DistributedQuantity = inventoryLog.DistributedQuantity,
                IsPublished = inventoryLog.IsPublished,
                CollectedAt = inventoryLog.CollectedAt
            };
        }

        public async Task<IReadOnlyList<InventoryLogResponseDto>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT inventory_log_id, donation_id, donation_request_id, distribution_center_user_id, collected_amount, distributed_quantity, is_published, collected_at
                FROM dbo.inventory_logs
                WHERE distribution_center_user_id = @DistributionCenterUserId
                ORDER BY collected_at DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DistributionCenterUserId", distributionCenterUserId);

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
            if (!await reader.ReadAsync())
                return null;

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
    }
}