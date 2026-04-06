using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    // new — repository interface for inventory logs (DC collect tracking)
    public interface IInventoryLogRepository
    {
        Task<InventoryLogResponseDto> CreateAsync(InventoryLog inventoryLog);
        Task UpdateIsPublishedAsync(int inventoryLogId, bool isPublished);
        Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync();
        Task<InventoryLog?> GetByIdAsync(int inventoryLogId);
        Task UpdateDistributedQuantityAsync(int inventoryLogId, decimal addedQuantity);
        Task<IEnumerable<CenterWithDonationsDto>> GetCentersWithPublishedDonationsAsync();
    }
}
