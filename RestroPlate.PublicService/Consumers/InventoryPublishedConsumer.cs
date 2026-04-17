using MassTransit;
using RestroPlate.PublicService.Models;

namespace RestroPlate.PublicService.Consumers;

public class InventoryPublishedConsumer : IConsumer<InventoryPublishedEvent>
{
    private readonly ConnectionFactory _connectionFactory;

    public InventoryPublishedConsumer(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task Consume(ConsumeContext<InventoryPublishedEvent> context)
    {
        const string insertSql = @"
            INSERT INTO PublishedDonationsView
                (DonationId, CenterId, CenterName, CenterAddress, FoodType, Quantity, Unit, CollectedAt)
            VALUES
                (@DonationId, @CenterId, @CenterName, @CenterAddress, @FoodType, @Quantity, @Unit, @CollectedAt);";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(context.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = insertSql;
        command.Parameters.AddWithValue("@DonationId", context.Message.DonationId);
        command.Parameters.AddWithValue("@CenterId", context.Message.CenterId);
        command.Parameters.AddWithValue("@CenterName", context.Message.CenterName);
        command.Parameters.AddWithValue("@CenterAddress", context.Message.CenterAddress);
        command.Parameters.AddWithValue("@FoodType", context.Message.FoodType);
        command.Parameters.AddWithValue("@Quantity", context.Message.Quantity);
        command.Parameters.AddWithValue("@Unit", context.Message.Unit);
        command.Parameters.AddWithValue("@CollectedAt", context.Message.CollectedAt);

        await command.ExecuteNonQueryAsync(context.CancellationToken);
    }
}