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
                (donation_id, donation_request_id, distribution_center_user_id, collected_amount)
                OUTPUT INSERTED.inventory_log_id, INSERTED.collected_at
                VALUES
                (@DonationId, @DonationRequestId, @DistributionCenterUserId, @CollectedAmount);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", inventoryLog.DonationId);
            command.Parameters.AddWithValue("@DonationRequestId", (object?)inventoryLog.DonationRequestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DistributionCenterUserId", inventoryLog.DistributionCenterUserId);
            command.Parameters.AddWithValue("@CollectedAmount", inventoryLog.CollectedAmount);

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
                CollectedAt = inventoryLog.CollectedAt
            };
        }
    }
}
