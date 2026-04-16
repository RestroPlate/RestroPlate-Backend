using RestroPlate.DonationService.Models;
using RestroPlate.DonationService.Models.DTOs;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Services
{
    public class DonationRequestService : IDonationRequestService
    {
        private static readonly HashSet<string> AllowedRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending",
            "completed"
        };

        private readonly IDonationRequestRepository _donationRequestRepository;

        public DonationRequestService(IDonationRequestRepository donationRequestRepository)
        {
            _donationRequestRepository = donationRequestRepository;
        }

        public async Task<DonationRequestResponseDto> CreateDonationRequestAsync(int distributionCenterUserId, CreateDonationRequestDto request)
        {
            ValidateCreateRequest(request);

            var donationRequest = new DonationRequest
            {
                DistributionCenterUserId = distributionCenterUserId,
                FoodType = request.FoodType.Trim(),
                RequestedQuantity = request.RequestedQuantity,
                Unit = request.Unit.Trim(),
                Status = "pending"
            };

            var createdDonationRequest = await _donationRequestRepository.CreateAsync(donationRequest);
            return MapToResponse(createdDonationRequest);
        }

        public async Task<IReadOnlyList<DonationRequestResponseDto>> GetAvailableRequestsAsync(string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var requests = await _donationRequestRepository.GetAvailableAsync(normalizedStatus);
            return requests.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<DonationRequestResponseDto>> GetOutgoingRequestsAsync(int distributionCenterUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var requests = await _donationRequestRepository.GetByDistributionCenterUserIdAsync(distributionCenterUserId, normalizedStatus);
            return requests.Select(MapToResponse).ToList();
        }

        public async Task<DonationRequestResponseDto> UpdateDonationRequestQuantityAsync(int donationRequestId, decimal donatedQuantity)
        {
            if (donatedQuantity <= 0)
                throw new ArgumentException("DonatedQuantity must be greater than zero.");

            var donationRequest = await _donationRequestRepository.GetByIdAsync(donationRequestId);
            if (donationRequest == null)
                throw new KeyNotFoundException("Donation Request not found.");

            donationRequest.DonatedQuantity += donatedQuantity;
            donationRequest.Status = donationRequest.DonatedQuantity >= donationRequest.RequestedQuantity ? "completed" : "pending";

            var updated = await _donationRequestRepository.UpdateAsync(donationRequest);
            if (!updated)
                throw new KeyNotFoundException("Donation Request not found.");

            return MapToResponse(donationRequest);
        }

        private static DonationRequestResponseDto MapToResponse(DonationRequest donationRequest) => new()
        {
            DonationRequestId = donationRequest.DonationRequestId,
            DistributionCenterUserId = donationRequest.DistributionCenterUserId,
            RequestedQuantity = donationRequest.RequestedQuantity,
            DonatedQuantity = donationRequest.DonatedQuantity,
            Status = donationRequest.Status,
            CreatedAt = donationRequest.CreatedAt,
            FoodType = donationRequest.FoodType,
            Unit = donationRequest.Unit
        };

        private static void ValidateCreateRequest(CreateDonationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FoodType))
                throw new ArgumentException("Food type is required.");

            if (request.RequestedQuantity <= 0)
                throw new ArgumentException("Requested quantity must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.Unit))
                throw new ArgumentException("Unit is required.");
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalizedStatus = status.Trim().ToLowerInvariant();
            if (!AllowedRequestStatuses.Contains(normalizedStatus))
                throw new ArgumentException("Status must be one of: pending, completed.");

            return normalizedStatus;
        }
    }
}