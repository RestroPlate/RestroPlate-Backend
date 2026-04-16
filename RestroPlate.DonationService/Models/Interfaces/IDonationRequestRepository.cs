using RestroPlate.DonationService.Models;

namespace RestroPlate.DonationService.Models.Interfaces
{
    public interface IDonationRequestRepository
    {
        Task<DonationRequest> CreateAsync(DonationRequest donationRequest);
        Task<IReadOnlyList<DonationRequest>> GetAvailableAsync(string? status = null);
        Task<IReadOnlyList<DonationRequest>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId, string? status = null);
        Task<DonationRequest?> GetByIdAsync(int donationRequestId);
        Task<bool> UpdateAsync(DonationRequest donationRequest);
    }
}