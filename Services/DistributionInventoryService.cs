using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class DistributionInventoryService : IDistributionInventoryService
    {
        private readonly IDistributionInventoryRepository _inventoryRepository;
        private readonly IDonationRequestRepository _donationRequestRepository;

        public DistributionInventoryService(
            IDistributionInventoryRepository inventoryRepository,
            IDonationRequestRepository donationRequestRepository)
        {
            _inventoryRepository = inventoryRepository;
            _donationRequestRepository = donationRequestRepository;
        }

        public async Task<DistributionInventoryResponseDto> CollectDonationAsync(int donationRequestId, UpdateCollectedQuantityDto dto)
        {
            if (dto.CollectedQuantity <= 0)
                throw new ArgumentException("Collected quantity must be greater than zero.");

            // Verify the donation request exists
            var donationRequest = await _donationRequestRepository.GetByIdAsync(donationRequestId);
            if (donationRequest == null)
                throw new KeyNotFoundException($"Donation request with ID {donationRequestId} not found.");

            // Upsert the inventory record
            var inventory = new DistributionInventory
            {
                DonationRequestId = donationRequestId,
                CollectedQuantity = dto.CollectedQuantity
            };

            var upsertedInventory = await _inventoryRepository.UpsertAsync(inventory);

            // Update donation request status to 'collected'
            donationRequest.Status = "collected";
            await _donationRequestRepository.UpdateAsync(donationRequest);

            return MapToResponse(upsertedInventory, donationRequest.Status);
        }

        public async Task<IReadOnlyList<DistributionInventoryResponseDto>> GetInventoryByDistributionCenterAsync(int distributionCenterUserId)
        {
            var inventoryList = await _inventoryRepository.GetByDistributionCenterUserIdAsync(distributionCenterUserId);

            var responseDtos = new List<DistributionInventoryResponseDto>();
            foreach (var inventory in inventoryList)
            {
                var donationRequest = await _donationRequestRepository.GetByIdAsync(inventory.DonationRequestId);
                var status = donationRequest?.Status ?? "unknown";
                responseDtos.Add(MapToResponse(inventory, status));
            }

            return responseDtos;
        }

        private static DistributionInventoryResponseDto MapToResponse(DistributionInventory inventory, string status) => new()
        {
            InventoryId = inventory.InventoryId,
            DonationRequestId = inventory.DonationRequestId,
            CollectedQuantity = inventory.CollectedQuantity,
            CollectionDate = inventory.CollectionDate,
            Status = status
        };
    }
}
