namespace RestroPlate.Models.Interfaces
{
    public interface IDonationClaimRepository
    {
        Task<int> CreateAsync(DonationClaim claim);
        Task<DonationClaim?> GetByIdAsync(int claimId);
        Task<IReadOnlyList<DonationClaim>> GetByDonationIdAsync(int donationId);
        Task<IReadOnlyList<DonationClaim>> GetByDonatorUserIdAsync(int donatorUserId);
        Task<IReadOnlyList<DonationClaim>> GetByCenterUserIdAsync(int centerUserId);
        Task<bool> UpdateStatusAsync(int claimId, string newStatus);
    }
}
