using RestroPlate.DonationService.Models.DTOs;

namespace RestroPlate.DonationService.Models.Interfaces
{
    public interface IDonationService
    {
        Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationDto request);
        Task<IReadOnlyList<DonationResponseDto>> GetUserDonationsAsync(int providerUserId, string? status = null);
        Task<DonationResponseDto> UpdateDonationAsync(int donationId, int providerUserId, UpdateDonationRequestDto request);
        Task DeleteDonationAsync(int donationId, int providerUserId);
        Task<IReadOnlyList<DonationResponseDto>> GetAvailableDonationsAsync(string? location, string? foodType, string? sortBy);
        Task<DonationResponseDto> RequestDonationAsync(int donationId, int distributionCenterUserId);
    }
}