namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRepository
    {
        Task<int> CreateAsync(Donation donation);
        Task<IReadOnlyList<Donation>> GetByUserIdAsync(int providerUserId, string? status = null);
        Task<Donation?> GetByIdAsync(int donationId, int providerUserId);
        Task<bool> UpdateAsync(Donation donation);
        Task<bool> DeleteAsync(int donationId, int providerUserId);
    }
}
