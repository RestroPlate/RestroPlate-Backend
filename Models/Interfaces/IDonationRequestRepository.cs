namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRequestRepository
    {
        Task<DonationRequest> CreateAsync(DonationRequest donationRequest);
        Task<IReadOnlyList<DonationRequest>> GetByProviderUserIdAsync(int providerUserId, string? status = null);
        Task<IReadOnlyList<DonationRequest>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId, string? status = null);
    }
}
