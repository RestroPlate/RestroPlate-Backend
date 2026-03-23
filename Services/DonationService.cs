using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class DonationService : IDonationService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "available",
            "requested",
            "collected",
            "completed"
        };

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationRequestRepository _donationRequestRepository;

        public DonationService(IDonationRepository donationRepository, IDonationRequestRepository donationRequestRepository)
        {
            _donationRepository = donationRepository;
            _donationRequestRepository = donationRequestRepository;
        }

        public async Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationDto request)
        {
            ValidateCreateRequest(request);

            if (request.DonationRequestId.HasValue)
            {
                var donationRequest = await _donationRequestRepository.GetByIdAsync(request.DonationRequestId.Value);
                if (donationRequest == null)
                    throw new KeyNotFoundException("Associated Donation Request not found.");
                if (donationRequest.Status != "pending")
                    throw new InvalidOperationException("Can only fulfill pending requests.");
                    
                // Optional: Check if fulfillment goes over (left mostly unrestricted for simple logic, but could add check here)
            }

            var donation = new Donation
            {
                DonationRequestId = request.DonationRequestId,
                ProviderUserId = providerUserId,
                FoodType = request.FoodType.Trim(),
                Quantity = request.Quantity,
                Unit = request.Unit.Trim(),
                ExpirationDate = request.ExpirationDate,
                PickupAddress = request.PickupAddress.Trim(),
                AvailabilityTime = request.AvailabilityTime.Trim(),
                Status = "available"
            };

            var donationId = await _donationRepository.CreateAsync(donation);
            donation.DonationId = donationId;

            return MapToResponse(donation);
        }

        public async Task<IReadOnlyList<DonationResponseDto>> GetUserDonationsAsync(int providerUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var donations = await _donationRepository.GetByUserIdAsync(providerUserId, normalizedStatus);
            return donations.Select(MapToResponse).ToList();
        }

        public async Task<DonationResponseDto> UpdateDonationAsync(int donationId, int providerUserId, UpdateDonationRequestDto request)
        {
            ValidateUpdateRequest(request);

            var existingDonation = await _donationRepository.GetByIdAsync(donationId, providerUserId);
            if (existingDonation is null)
                throw new KeyNotFoundException("Donation not found.");

            if (!string.Equals(existingDonation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only available donations can be updated.");

            existingDonation.FoodType = request.FoodType.Trim();
            existingDonation.Quantity = request.Quantity;
            existingDonation.Unit = request.Unit.Trim();
            existingDonation.ExpirationDate = request.ExpirationDate;
            existingDonation.PickupAddress = request.PickupAddress.Trim();
            existingDonation.AvailabilityTime = request.AvailabilityTime.Trim();

            var updated = await _donationRepository.UpdateAsync(existingDonation);
            if (!updated)
                throw new KeyNotFoundException("Donation not found.");

            return MapToResponse(existingDonation);
        }

        public async Task DeleteDonationAsync(int donationId, int providerUserId)
        {
            var existingDonation = await _donationRepository.GetByIdAsync(donationId, providerUserId);
            if (existingDonation is null)
                throw new KeyNotFoundException("Donation not found.");

            if (!string.Equals(existingDonation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only available donations can be deleted.");

            var deleted = await _donationRepository.DeleteAsync(donationId, providerUserId);
            if (!deleted)
                throw new KeyNotFoundException("Donation not found.");
        }

        public async Task<IReadOnlyList<DonationResponseDto>> GetAvailableDonationsAsync(string? location, string? foodType, string? sortBy)
        {
            var normalizedSort = NormalizeSortBy(sortBy);
            var donations = await _donationRepository.GetAvailableAsync(location, foodType, normalizedSort);
            return donations.Select(MapToResponse).ToList();
        }

        private static DonationResponseDto MapToResponse(Donation donation) => new()
        {
            DonationId = donation.DonationId,
            DonationRequestId = donation.DonationRequestId,
            ProviderUserId = donation.ProviderUserId,
            FoodType = donation.FoodType,
            Quantity = donation.Quantity,
            Unit = donation.Unit,
            ExpirationDate = donation.ExpirationDate,
            PickupAddress = donation.PickupAddress,
            AvailabilityTime = donation.AvailabilityTime,
            Status = donation.Status,
            CreatedAt = donation.CreatedAt
        };

        private static void ValidateCreateRequest(CreateDonationDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FoodType))
                throw new ArgumentException("Food type is required.");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.Unit))
                throw new ArgumentException("Unit is required.");

            if (request.ExpirationDate <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future.");

            if (string.IsNullOrWhiteSpace(request.PickupAddress))
                throw new ArgumentException("Pickup address is required.");

            if (string.IsNullOrWhiteSpace(request.AvailabilityTime))
                throw new ArgumentException("Availability time is required.");
        }

        private static void ValidateUpdateRequest(UpdateDonationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FoodType))
                throw new ArgumentException("Food type is required.");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.Unit))
                throw new ArgumentException("Unit is required.");

            if (request.ExpirationDate <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future.");

            if (string.IsNullOrWhiteSpace(request.PickupAddress))
                throw new ArgumentException("Pickup address is required.");

            if (string.IsNullOrWhiteSpace(request.AvailabilityTime))
                throw new ArgumentException("Availability time is required.");
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalizedStatus = status.Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalizedStatus))
                throw new ArgumentException("Status must be one of: available, requested, collected, completed.");

            return normalizedStatus;
        }

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt",
            "expirationDate"
        };

        private static string? NormalizeSortBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return null;

            var normalized = sortBy.Trim().ToLowerInvariant();
            if (!AllowedSortFields.Contains(sortBy.Trim()))
                throw new ArgumentException("SortBy must be one of: createdAt, expirationDate.");

            return normalized;
        }
    }
}
