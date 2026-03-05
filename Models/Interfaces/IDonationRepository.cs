namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRepository
    {
        Task<int> CreateAsync(Donation donation);
        Task<IReadOnlyList<Donation>> GetByProviderUserIdAsync(int providerUserId);
    }
}
