using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationService
    {
        // exists & correct — skipped
        Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationDto request);
        Task<IReadOnlyList<DonationResponseDto>> GetUserDonationsAsync(int providerUserId, string? status = null);
        Task<DonationResponseDto> UpdateDonationAsync(int donationId, int providerUserId, UpdateDonationRequestDto request);
        Task DeleteDonationAsync(int donationId, int providerUserId);
        Task<IReadOnlyList<DonationResponseDto>> GetAvailableDonationsAsync(string? location, string? foodType, string? sortBy);
        
        // new — DC views their claimed donations (inventory)
        Task<IReadOnlyList<DonationResponseDto>> GetCenterInventoryAsync(int distributionCenterUserId);

        // new — Flow 1: DC requests a donation (available → requested)
        Task<DonationResponseDto> RequestDonationAsync(int donationId, int distributionCenterUserId);

        // new — shared (Flow 1 + Flow 2): DC collects; logs to inventory; updates DonationRequest if linked
        Task<InventoryLogResponseDto> CollectDonationAsync(int donationId, int distributionCenterUserId, CollectDonationDto request);
        
        // new — publishing inventory
        Task UpdateInventoryPublishStatusAsync(int inventoryLogId, int distributionCenterUserId, bool isPublished);
        Task<IReadOnlyList<InventoryLogResponseDto>> GetPublishedInventoryAsync();
    }
}
