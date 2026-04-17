using RestroPlate.InventoryService.DTOs;

namespace RestroPlate.InventoryService.Models.Interfaces
{
    public interface IInventoryService
    {
        Task<IReadOnlyList<InventoryLogResponseDto>> GetInventoryAsync(int distributionCenterUserId);
        Task<InventoryLogResponseDto> CollectDonationAsync(int donationId, int distributionCenterUserId, CollectDonationDto request);
        Task UpdateInventoryPublishStatusAsync(int inventoryLogId, int distributionCenterUserId, bool isPublished, string? centerName = null, string? centerAddress = null);
        Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync();
        Task<InventoryLogResponseDto> UpdateDistributedQuantityAsync(int inventoryLogId, int distributionCenterUserId, decimal addedQuantity);
    }
}