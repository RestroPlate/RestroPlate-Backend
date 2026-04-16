using RestroPlate.InventoryService.DTOs;
using RestroPlate.InventoryService.Models;

namespace RestroPlate.InventoryService.Models.Interfaces
{
    public interface IInventoryLogRepository
    {
        Task<InventoryLogResponseDto> CreateAsync(InventoryLog inventoryLog);
        Task<IReadOnlyList<InventoryLogResponseDto>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId);
        Task UpdateIsPublishedAsync(int inventoryLogId, bool isPublished);
        Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync();
        Task<InventoryLog?> GetByIdAsync(int inventoryLogId);
        Task UpdateDistributedQuantityAsync(int inventoryLogId, decimal addedQuantity);
    }
}