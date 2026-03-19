using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDistributionInventoryService
    {
        Task<DistributionInventoryResponseDto> CollectDonationAsync(int donationRequestId, UpdateCollectedQuantityDto dto);
        Task<IReadOnlyList<DistributionInventoryResponseDto>> GetInventoryByDistributionCenterAsync(int distributionCenterUserId);
    }
}
