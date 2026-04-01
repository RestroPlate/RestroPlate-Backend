namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRepository
    {
        // exists & correct — skipped
        Task<int> CreateAsync(Donation donation);
        Task<IReadOnlyList<Donation>> GetByUserIdAsync(int providerUserId, string? status = null);
        Task<Donation?> GetByIdAsync(int donationId);
        Task<Donation?> GetByIdAsync(int donationId, int providerUserId);
        Task<bool> UpdateAsync(Donation donation);
        Task<bool> DeleteAsync(int donationId, int providerUserId);
        Task<IReadOnlyList<Donation>> GetAvailableAsync(string? location, string? foodType, string? sortBy);
        Task<decimal> GetTotalFulfilledQuantityAsync(int donationRequestId);

        // new — atomic status-only update used by request and collect transitions
        Task<bool> UpdateStatusAsync(int donationId, string newStatus);

        // new — atomic update of status + claimed_by_center_user_id (used by claim acceptance)
        Task<bool> UpdateStatusAndClaimedByAsync(int donationId, string newStatus, int centerUserId);
    }
}
