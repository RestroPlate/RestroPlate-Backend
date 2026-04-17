using MassTransit;
using RestroPlate.EventContracts;
using RestroPlate.InventoryService.Models;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Consumers;

public class DonationCreatedConsumer : IConsumer<DonationCreatedEvent>
{
    private readonly IInventoryLogRepository _inventoryLogRepository;

    public DonationCreatedConsumer(IInventoryLogRepository inventoryLogRepository)
    {
        _inventoryLogRepository = inventoryLogRepository;
    }

    public async Task Consume(ConsumeContext<DonationCreatedEvent> context)
    {
        var inventoryLog = new InventoryLog
        {
            DonationId = context.Message.DonationId,
            DonationRequestId = null,
            DistributionCenterUserId = context.Message.ProviderUserId,
            CollectedAmount = 0,
            DistributedQuantity = 0,
            IsPublished = false
        };

        await _inventoryLogRepository.CreateAsync(inventoryLog);
    }
}
