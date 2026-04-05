using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class DonationService : IDonationService
    {
        // ─── Valid statuses and allowed transitions ─────────────────────────────
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "available", "requested", "collected"
        };

        // Status machine: key = current status allowed for that transition
        private static readonly HashSet<string> CollectableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "requested"
        };

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationRequestRepository _donationRequestRepository;
        private readonly IInventoryLogRepository _inventoryLogRepository;
        private readonly IUserRepository _userRepository;

        public DonationService(
            IDonationRepository donationRepository,
            IDonationRequestRepository donationRequestRepository,
            IInventoryLogRepository inventoryLogRepository,
            IUserRepository userRepository)
        {
            _donationRepository = donationRepository;
            _donationRequestRepository = donationRequestRepository;
            _inventoryLogRepository = inventoryLogRepository;
            _userRepository = userRepository;
        }

        // ───────────────────────────────────────────────────────────────────────
        // Flow 1: POST /donations — Donor creates a donation; status = available
        // Flow 2: POST /donations (with DonationRequestId) — Donor fulfills a
        //         pending request; status = requested; DonationRequest quantity updated
        // ───────────────────────────────────────────────────────────────────────
        public async Task<DonationResponseDto> CreateDonationAsync(int providerUserId, CreateDonationDto request)
        {
            ValidateCreateRequest(request);

            // modified: split Flow 1 vs Flow 2 based on whether a DonationRequestId is supplied
            if (request.DonationRequestId.HasValue)
            {
                // Flow 2 — fulfil a pending DC request
                var donationRequest = await _donationRequestRepository.GetByIdAsync(request.DonationRequestId.Value);
                if (donationRequest == null)
                    throw new KeyNotFoundException("Associated Donation Request not found.");

                // Guard: only pending requests can be fulfilled (409 on wrong transition)
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
                    Status = "requested", // modified: Flow 2 donations start in 'requested' state
                    ClaimedByCenterUserId = donationRequest.DistributionCenterUserId
                };

                var donationId = await _donationRepository.CreateAsync(donation);
                donation.DonationId = donationId;

                // Update DonationRequest: increment donated quantity, set completed if fully fulfilled
                donationRequest.DonatedQuantity += request.Quantity;

                var remaining = donationRequest.RequestedQuantity - donationRequest.DonatedQuantity;
                donationRequest.Status = remaining <= 0 ? "completed" : "pending";

                await _donationRequestRepository.UpdateAsync(donationRequest);

                // emit: donation_request.updated (logged as console event; real event bus out of scope)
                Console.WriteLine($"[event:donation_request.updated] DonationRequestId={donationRequest.DonationRequestId} " +
                                  $"Remaining={Math.Max(0, remaining):F2} Status={donationRequest.Status}");

                return MapToResponse(donation);
            }
            else
            {
                // Flow 1 — donor creates a standalone donation
                var donation = new Donation
                {
                    DonationRequestId = null,
                    ProviderUserId = providerUserId,
                    FoodType = request.FoodType.Trim(),
                    Quantity = request.Quantity,
                    Unit = request.Unit.Trim(),
                    ExpirationDate = request.ExpirationDate,
                    PickupAddress = request.PickupAddress.Trim(),
                    AvailabilityTime = request.AvailabilityTime.Trim(),
                    Status = "available" // exists & correct — Flow 1 donations start available
                };

                var donationId = await _donationRepository.CreateAsync(donation);
                donation.DonationId = donationId;
                return MapToResponse(donation);
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // Flow 1: PATCH /donations/:id/request — DC requests a donation
        //         Guard: must be in 'available' state → transitions to 'requested'
        //         emits donation.requested to notify the donor
        // new
        // ───────────────────────────────────────────────────────────────────────
        public async Task<DonationResponseDto> RequestDonationAsync(int donationId, int distributionCenterUserId)
        {
            var donation = await _donationRepository.GetByIdAsync(donationId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found.");

            // Guard invalid transition → 409
            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Only available donations can be requested. Current status: {donation.Status}");

            var updated = await _donationRepository.UpdateStatusAsync(donationId, "requested");
            if (!updated)
                throw new KeyNotFoundException("Donation not found.");

            donation.Status = "requested";

            // emit: donation.requested — notifies the donor
            Console.WriteLine($"[event:donation.requested] DonationId={donationId} RequestedBy=DC:{distributionCenterUserId}");

            return MapToResponse(donation);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Shared (Flow 1 + Flow 2): PATCH /donations/:id/collect — DC collects
        //   - Guard: must be in 'requested' state → transitions to 'collected'
        //   - Records inventory log entry for the DC
        //   - If linked to a DonationRequest: updates collected_amount (tracked via collected donation)
        // new
        // ───────────────────────────────────────────────────────────────────────
        public async Task<InventoryLogResponseDto> CollectDonationAsync(int donationId, int distributionCenterUserId, CollectDonationDto request)
        {
            if (request.CollectedAmount <= 0)
                throw new ArgumentException("CollectedAmount must be greater than zero.");

            var donation = await _donationRepository.GetByIdAsync(donationId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found.");

            // Guard invalid transition → 409
            if (!CollectableStatuses.Contains(donation.Status))
                throw new InvalidOperationException($"Only requested donations can be collected. Current status: {donation.Status}");

            // Transition to collected
            var updated = await _donationRepository.UpdateStatusAsync(donationId, "collected");
            if (!updated)
                throw new KeyNotFoundException("Donation not found.");

            donation.Status = "collected";

            // Create inventory log entry
            var inventoryLog = new InventoryLog
            {
                DonationId = donationId,
                DonationRequestId = donation.DonationRequestId,
                DistributionCenterUserId = distributionCenterUserId,
                CollectedAmount = request.CollectedAmount
            };

            var logEntry = await _inventoryLogRepository.CreateAsync(inventoryLog);

            Console.WriteLine($"[event:donation.collected] DonationId={donationId} CollectedBy=DC:{distributionCenterUserId} Amount={request.CollectedAmount}");

            return logEntry;
        }

        // exists & correct — skipped
        public async Task<IReadOnlyList<DonationResponseDto>> GetUserDonationsAsync(int providerUserId, string? status = null)
        {
            var normalizedStatus = NormalizeStatus(status);
            var donations = await _donationRepository.GetByUserIdAsync(providerUserId, normalizedStatus);

            var responseList = new List<DonationResponseDto>();

            foreach (var donation in donations)
            {
                var responseDto = MapToResponse(donation);

                if (donation.ClaimedByCenterUserId.HasValue)
                {
                    var centerUser = await _userRepository.GetByIdAsync(donation.ClaimedByCenterUserId.Value);
                    if (centerUser != null)
                    {
                        responseDto.CenterDetails = new CenterDetailsDto
                        {
                            UserId = centerUser.UserId,
                            Name = centerUser.Name,
                            Email = centerUser.Email,
                            PhoneNumber = centerUser.PhoneNumber,
                            Address = centerUser.Address
                        };
                    }
                }

                responseList.Add(responseDto);
            }

            return responseList;
        }

        // exists & correct — skipped
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

        // exists & correct — skipped
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

        // exists & correct — skipped
        public async Task<IReadOnlyList<DonationResponseDto>> GetAvailableDonationsAsync(string? location, string? foodType, string? sortBy)
        {
            var normalizedSort = NormalizeSortBy(sortBy);
            var donations = await _donationRepository.GetAvailableAsync(location, foodType, normalizedSort);
            return donations.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<DonationResponseDto>> GetCenterInventoryAsync(int distributionCenterUserId)
        {
            var donations = await _donationRepository.GetCenterInventoryAsync(distributionCenterUserId);

            var responseList = new List<DonationResponseDto>();
            foreach (var donation in donations)
            {
                var responseDto = MapToResponse(donation);

                // Fetch donator details to include in response if needed (optional based on DTO)
                var providerUser = await _userRepository.GetByIdAsync(donation.ProviderUserId);
                if (providerUser != null)
                {
                    // If we want to include provider details we could just do it via CenterDetails
                    // But maybe simply mapping is enough as requested. We'll simply map.
                    // Wait, Donor info is part of CenterDetailsDto in GetUserDonationsAsync so maybe not here.
                }

                responseList.Add(responseDto);
            }

            return responseList;
        }

        // ── Mapping ────────────────────────────────────────────────────────────
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
            ClaimedByCenterUserId = donation.ClaimedByCenterUserId,
            CreatedAt = donation.CreatedAt
        };

        // ── Validation ─────────────────────────────────────────────────────────
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
                throw new ArgumentException("Status must be one of: available, requested, collected.");
            return normalizedStatus;
        }

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt", "expirationDate"
        };

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
