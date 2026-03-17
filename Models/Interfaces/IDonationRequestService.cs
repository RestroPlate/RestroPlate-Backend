using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRequestService
    {
        Task<DonationRequestResponseDto> CreateDonationRequestAsync(int distributionCenterUserId, CreateDonationRequestRequestDto request);
        Task<IReadOnlyList<DonationRequestResponseDto>> GetProviderRequestsAsync(int providerUserId, string? status = null);
        Task<IReadOnlyList<DonationRequestResponseDto>> GetOutgoingRequestsAsync(int distributionCenterUserId, string? status = null);
    }
}
