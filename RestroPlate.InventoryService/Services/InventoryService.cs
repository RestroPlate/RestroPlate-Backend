using RestroPlate.InventoryService.DTOs;
using RestroPlate.InventoryService.Models;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryLogRepository _inventoryLogRepository;

        public InventoryService(IInventoryLogRepository inventoryLogRepository)
        {
            _inventoryLogRepository = inventoryLogRepository;
        }

        public async Task<IReadOnlyList<InventoryLogResponseDto>> GetInventoryAsync(int distributionCenterUserId)
        {
            return await _inventoryLogRepository.GetByDistributionCenterUserIdAsync(distributionCenterUserId);
        }

        public async Task<InventoryLogResponseDto> CollectDonationAsync(int donationId, int distributionCenterUserId, CollectDonationDto request)
        {
            if (request.CollectedAmount <= 0)
                throw new ArgumentException("CollectedAmount must be greater than zero.");

            var inventoryLog = new InventoryLog
            {
                DonationId = donationId,
                DistributionCenterUserId = distributionCenterUserId,
                CollectedAmount = request.CollectedAmount,
                DistributedQuantity = 0,
                IsPublished = false
            };

            return await _inventoryLogRepository.CreateAsync(inventoryLog);
        }

        public async Task UpdateInventoryPublishStatusAsync(int inventoryLogId, int distributionCenterUserId, bool isPublished)
        {
            var log = await _inventoryLogRepository.GetByIdAsync(inventoryLogId)
                ?? throw new KeyNotFoundException($"Inventory log with ID {inventoryLogId} not found.");

            if (log.DistributionCenterUserId != distributionCenterUserId)
                throw new UnauthorizedAccessException("You are not authorized to publish this inventory.");

            await _inventoryLogRepository.UpdateIsPublishedAsync(inventoryLogId, isPublished);
        }

        public async Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync()
        {
            return await _inventoryLogRepository.GetPublishedInventoryAsync();
        }

        public async Task<InventoryLogResponseDto> UpdateDistributedQuantityAsync(int inventoryLogId, int distributionCenterUserId, decimal addedQuantity)
        {
            if (addedQuantity <= 0)
                throw new ArgumentException("Distributed quantity must be greater than zero.");

            var log = await _inventoryLogRepository.GetByIdAsync(inventoryLogId)
                ?? throw new KeyNotFoundException($"Inventory log with ID {inventoryLogId} not found.");

            if (log.DistributionCenterUserId != distributionCenterUserId)
                throw new UnauthorizedAccessException("You are not authorized to update this inventory.");

            var newTotalDistributed = log.DistributedQuantity + addedQuantity;
            if (newTotalDistributed > log.CollectedAmount)
            {
                throw new InvalidOperationException(
                    $"Cannot distribute more than collected amount ({log.CollectedAmount:F2}). Current distributed: {log.DistributedQuantity:F2}, additional requested: {addedQuantity:F2}");
            }

            await _inventoryLogRepository.UpdateDistributedQuantityAsync(inventoryLogId, addedQuantity);
            var updated = await _inventoryLogRepository.GetByIdAsync(inventoryLogId)
                ?? throw new KeyNotFoundException($"Inventory log with ID {inventoryLogId} not found.");

            return new InventoryLogResponseDto
            {
                InventoryLogId = updated.InventoryLogId,
                DonationId = updated.DonationId,
                DonationRequestId = updated.DonationRequestId,
                DistributionCenterUserId = updated.DistributionCenterUserId,
                CollectedAmount = updated.CollectedAmount,
                DistributedQuantity = updated.DistributedQuantity,
                IsPublished = updated.IsPublished,
                CollectedAt = updated.CollectedAt
            };
        }
    }
}