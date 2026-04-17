using RestroPlate.DonationService.Models;
using RestroPlate.DonationService.Models.DTOs;
using RestroPlate.DonationService.Models.Interfaces;
using MassTransit;
using RestroPlate.EventContracts;

namespace RestroPlate.DonationService.Services
{
    public class DonationService : IDonationService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "available", "requested", "collected", "completed"
        };

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt", "expirationDate"
        };

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationRequestRepository _donationRequestRepository;
        private readonly IDonationImageRepository _imageRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public DonationService(
            IDonationRepository donationRepository,
            IDonationRequestRepository donationRequestRepository,
            IDonationImageRepository imageRepository,
            IPublishEndpoint publishEndpoint)
        {
            _donationRepository = donationRepository;
            _donationRequestRepository = donationRequestRepository;
            _imageRepository = imageRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationDto request)
        {
            ValidateCreateRequest(request);

            if (request.DonationRequestId.HasValue)
            {
                var donationRequest = await _donationRequestRepository.GetByIdAsync(request.DonationRequestId.Value);
                if (donationRequest == null)
                    throw new KeyNotFoundException("Associated Donation Request not found.");

                if (!string.Equals(donationRequest.Status, "pending", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only pending donation requests can be fulfilled. Current status: " + donationRequest.Status);

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
                    Status = "requested",
                    ClaimedByCenterUserId = donationRequest.DistributionCenterUserId
                };

                var donationId = await _donationRepository.CreateAsync(donation);
                donation.DonationId = donationId;

                await _publishEndpoint.Publish(new DonationCreatedEvent(
                    donation.DonationId,
                    donation.ProviderUserId,
                    donation.FoodType,
                    (double)donation.Quantity,
                    donation.Unit));

                donationRequest.DonatedQuantity += request.Quantity;
                var remaining = donationRequest.RequestedQuantity - donationRequest.DonatedQuantity;
                donationRequest.Status = remaining <= 0 ? "completed" : "pending";

                await _donationRequestRepository.UpdateAsync(donationRequest);

                return await MapToResponseAsync(donation);
            }

            var standaloneDonation = new Donation
            {
                DonationRequestId = null,
                ProviderUserId = providerUserId,
                FoodType = request.FoodType.Trim(),
                Quantity = request.Quantity,
                Unit = request.Unit.Trim(),
                ExpirationDate = request.ExpirationDate,
                PickupAddress = request.PickupAddress.Trim(),
                AvailabilityTime = request.AvailabilityTime.Trim(),
                Status = "available"
            };

            var createdId = await _donationRepository.CreateAsync(standaloneDonation);
            standaloneDonation.DonationId = createdId;

            await _publishEndpoint.Publish(new DonationCreatedEvent(
                standaloneDonation.DonationId,
                standaloneDonation.ProviderUserId,
                standaloneDonation.FoodType,
                (double)standaloneDonation.Quantity,
                standaloneDonation.Unit));

            return await MapToResponseAsync(standaloneDonation);
        }

        public async Task<DonationResponseDto> RequestDonationAsync(int donationId, int distributionCenterUserId)
        {
            var donation = await _donationRepository.GetByIdAsync(donationId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found.");

            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Only available donations can be requested. Current status: {donation.Status}");

            var updated = await _donationRepository.UpdateStatusAndClaimedByAsync(donationId, "requested", distributionCenterUserId);
            if (!updated)
                throw new KeyNotFoundException("Donation not found.");

            donation.Status = "requested";
            donation.ClaimedByCenterUserId = distributionCenterUserId;

            return await MapToResponseAsync(donation);
        }

        public async Task<IReadOnlyList<DonationResponseDto>> GetUserDonationsAsync(int providerUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var donations = await _donationRepository.GetByUserIdAsync(providerUserId, normalizedStatus);
            var responseList = new List<DonationResponseDto>();

            foreach (var donation in donations)
                responseList.Add(await MapToResponseAsync(donation));

            return responseList;
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

            return await MapToResponseAsync(existingDonation);
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
            var result = new List<DonationResponseDto>();

            foreach (var donation in donations)
                result.Add(await MapToResponseAsync(donation));

            return result;
        }

        private async Task<DonationResponseDto> MapToResponseAsync(Donation donation)
        {
            var images = await _imageRepository.GetByDonationIdAsync(donation.DonationId);
            return new DonationResponseDto
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
                ClaimedByCenterUserId = donation.ClaimedByCenterUserId,
                IsPublished = donation.IsPublished,
                InventoryLogId = donation.InventoryLogId,
                CollectedAmount = donation.CollectedAmount,
                DistributedQuantity = donation.DistributedQuantity,
                CreatedAt = donation.CreatedAt,
                Images = images.ToList()
            };
        }

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

        private static string? NormalizeSortBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return null;

            if (!AllowedSortFields.Contains(sortBy.Trim()))
                throw new ArgumentException("SortBy must be one of: createdAt, expirationDate.");

            return sortBy.Trim().ToLowerInvariant();
        }
    }
}