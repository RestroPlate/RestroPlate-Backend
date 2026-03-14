using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationService
    {
        Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationRequestDto request);
        Task<IReadOnlyList<DonationResponseDto>> GetProviderDonationsAsync(int providerUserId);
    }
}
