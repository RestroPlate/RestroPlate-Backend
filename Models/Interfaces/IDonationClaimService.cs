using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationClaimService
    {
        Task<DonationClaimResponseDto> CreateClaimAsync(int centerUserId, CreateDonationClaimDto request);
        Task<DonationClaimResponseDto> UpdateClaimStatusAsync(int claimId, int donatorUserId, UpdateDonationClaimStatusDto request);
        Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByDonatorAsync(int donatorUserId);
        Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByCenterAsync(int centerUserId);
    }
}
