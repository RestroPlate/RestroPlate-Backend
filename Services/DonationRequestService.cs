using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class DonationRequestService : IDonationRequestService
    {
        private static readonly HashSet<string> AllowedRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending",
            "approved",
            "rejected"
        };

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationRequestRepository _donationRequestRepository;

        public DonationRequestService(IDonationRepository donationRepository, IDonationRequestRepository donationRequestRepository)
        {
            _donationRepository = donationRepository;
            _donationRequestRepository = donationRequestRepository;
        }

        public async Task<DonationRequestResponseDto> CreateDonationRequestAsync(int distributionCenterUserId, CreateDonationRequestRequestDto request)
        {
            ValidateCreateRequest(request);

            var donation = await _donationRepository.GetByIdAsync(request.DonationId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found.");

            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only available donations can be requested.");

            if (request.RequestedQuantity > donation.Quantity)
                throw new ArgumentException("Requested quantity cannot exceed available donation quantity.");

            var donationRequest = new DonationRequest
            {
                DonationId = donation.DonationId,
                ProviderUserId = donation.ProviderUserId,
                DistributionCenterUserId = distributionCenterUserId,
                RequestedQuantity = request.RequestedQuantity,
                Status = "pending",
                FoodType = donation.FoodType,
                Unit = donation.Unit
            };

            var createdDonationRequest = await _donationRequestRepository.CreateAsync(donationRequest);
            return MapToResponse(createdDonationRequest);
        }

        public async Task<IReadOnlyList<DonationRequestResponseDto>> GetProviderRequestsAsync(int providerUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var requests = await _donationRequestRepository.GetByProviderUserIdAsync(providerUserId, normalizedStatus);
            return requests.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<DonationRequestResponseDto>> GetOutgoingRequestsAsync(int distributionCenterUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var requests = await _donationRequestRepository.GetByDistributionCenterUserIdAsync(distributionCenterUserId, normalizedStatus);
            return requests.Select(MapToResponse).ToList();
        }

        private static DonationRequestResponseDto MapToResponse(DonationRequest donationRequest) => new()
        {
            DonationRequestId = donationRequest.DonationRequestId,
            DonationId = donationRequest.DonationId,
            ProviderUserId = donationRequest.ProviderUserId,
            DistributionCenterUserId = donationRequest.DistributionCenterUserId,
            RequestedQuantity = donationRequest.RequestedQuantity,
            Status = donationRequest.Status,
            CreatedAt = donationRequest.CreatedAt,
            FoodType = donationRequest.FoodType,
            Unit = donationRequest.Unit
        };

        private static void ValidateCreateRequest(CreateDonationRequestRequestDto request)
        {
            if (request.DonationId <= 0)
                throw new ArgumentException("Donation ID must be greater than zero.");

            if (request.RequestedQuantity <= 0)
                throw new ArgumentException("Requested quantity must be greater than zero.");
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalizedStatus = status.Trim().ToLowerInvariant();
            if (!AllowedRequestStatuses.Contains(normalizedStatus))
                throw new ArgumentException("Status must be one of: pending, approved, rejected.");

            return normalizedStatus;
        }
    }
}
