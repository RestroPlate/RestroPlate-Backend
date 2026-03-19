namespace RestroPlate.Models.Interfaces
{
    public interface IDistributionInventoryRepository
    {
        Task<DistributionInventory> UpsertAsync(DistributionInventory inventory);
        Task<DistributionInventory?> GetByDonationRequestIdAsync(int donationRequestId);
        Task<IReadOnlyList<DistributionInventory>> GetByDistributionCenterUserIdAsync(int distributionCenterUserId);
    }
}
