using RestroPlate.InventoryService.Models;

namespace RestroPlate.InventoryService.Models.Interfaces
{
    public interface IDonationClaimRepository
    {
        Task<int> CreateAsync(DonationClaim claim);
        Task<DonationClaim?> GetByIdAsync(int claimId);
        Task<IReadOnlyList<DonationClaim>> GetByDonatorUserIdAsync(int donatorUserId);
        Task<IReadOnlyList<DonationClaim>> GetByCenterUserIdAsync(int centerUserId);
        Task<bool> UpdateStatusAsync(int claimId, string newStatus);
    }
}