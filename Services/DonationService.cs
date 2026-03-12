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

        public DonationService(IDonationRepository donationRepository)
        {
            _donationRepository = donationRepository;
        }

        public async Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationRequestDto request)
        {
            ValidateCreateRequest(request);

            var donation = new Donation
            {
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

        private static DonationResponseDto MapToResponse(Donation donation) => new()
        {
            DonationId = donation.DonationId,
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

        private static void ValidateCreateRequest(CreateDonationRequestDto request)
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
    }
}
