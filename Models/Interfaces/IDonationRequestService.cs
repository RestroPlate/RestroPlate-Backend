using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationRequestService
    {
        Task<DonationRequestResponseDto> CreateDonationRequestAsync(int distributionCenterUserId, CreateDonationRequestDto request);
        Task<IReadOnlyList<DonationRequestResponseDto>> GetAvailableRequestsAsync(string? status = null);
        Task<IReadOnlyList<DonationRequestResponseDto>> GetOutgoingRequestsAsync(int distributionCenterUserId, string? status = null);
        Task<DonationRequestResponseDto> UpdateDonationRequestQuantityAsync(int donationRequestId, decimal donatedQuantity);
    }
}
