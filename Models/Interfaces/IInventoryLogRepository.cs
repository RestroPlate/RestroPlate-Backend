using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    // new — repository interface for inventory logs (DC collect tracking)
    public interface IInventoryLogRepository
    {
        Task<InventoryLogResponseDto> CreateAsync(InventoryLog inventoryLog);
    }
}
