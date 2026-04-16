using RestroPlate.DonationService.Models;

namespace RestroPlate.DonationService.Models.Interfaces
{
    public interface IDonationRepository
    {
        Task<int> CreateAsync(Donation donation);
        Task<IReadOnlyList<Donation>> GetByUserIdAsync(int providerUserId, string? status = null);
        Task<Donation?> GetByIdAsync(int donationId);
        Task<Donation?> GetByIdAsync(int donationId, int providerUserId);
        Task<bool> UpdateAsync(Donation donation);
        Task<bool> DeleteAsync(int donationId, int providerUserId);
        Task<IReadOnlyList<Donation>> GetAvailableAsync(string? location, string? foodType, string? sortBy);
        Task<decimal> GetTotalFulfilledQuantityAsync(int donationRequestId);
        Task<bool> UpdateStatusAsync(int donationId, string newStatus);
        Task<bool> UpdateStatusAndClaimedByAsync(int donationId, string newStatus, int centerUserId);
    }
}