using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class DonationClaimService : IDonationClaimService
    {
        private readonly IDonationClaimRepository _claimRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly IUserRepository _userRepository;

        public DonationClaimService(
            IDonationClaimRepository claimRepository,
            IDonationRepository donationRepository,
            IUserRepository userRepository)
        {
            _claimRepository = claimRepository;
            _donationRepository = donationRepository;
            _userRepository = userRepository;
        }

        // ───────────────────────────────────────────────────────────────────────
        // Create claim — only DISTRIBUTION_CENTER users
        // ───────────────────────────────────────────────────────────────────────
        public async Task<DonationClaimResponseDto> CreateClaimAsync(int centerUserId, CreateDonationClaimDto request)
        {
            // Validate donation exists
            var donation = await _donationRepository.GetByIdAsync(request.DonationId)
                ?? throw new KeyNotFoundException($"Donation {request.DonationId} not found.");

            // Validate donation is available
            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Donation {request.DonationId} is not available for claiming. Current status: {donation.Status}.");
            }

            var claim = new DonationClaim
            {
                DonationId = request.DonationId,
                CenterUserId = centerUserId,
                DonatorUserId = donation.ProviderUserId,
                Status = "pending"
            };

            var claimId = await _claimRepository.CreateAsync(claim);

            // Fetch the created claim to return full data
            var created = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new InvalidOperationException("Failed to retrieve created claim.");

            var centerUser = await _userRepository.GetByIdAsync(centerUserId);
            return MapToResponse(created, centerUser);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Update claim status — only the DONOR who owns the donation
        // ───────────────────────────────────────────────────────────────────────
        public async Task<DonationClaimResponseDto> UpdateClaimStatusAsync(int claimId, int donatorUserId, UpdateDonationClaimStatusDto request)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new KeyNotFoundException($"Claim {claimId} not found.");

            // Only the donator who owns the donation can accept/reject
            if (claim.DonatorUserId != donatorUserId)
            {
                throw new KeyNotFoundException($"Claim {claimId} not found.");
            }

            // Must be pending to transition
            if (!string.Equals(claim.Status, "pending", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Claim {claimId} is already {claim.Status}. Only pending claims can be updated.");
            }

            // Validate status value
            var normalizedStatus = request.Status?.Trim().ToLowerInvariant();
            if (normalizedStatus != "accepted" && normalizedStatus != "rejected")
            {
                throw new ArgumentException("Status must be 'accepted' or 'rejected'.");
            }

            await _claimRepository.UpdateStatusAsync(claimId, normalizedStatus);

            // If accepted, also update the donation: set status to 'requested' and record the DC that claimed it
            if (normalizedStatus == "accepted")
            {
                await _donationRepository.UpdateStatusAndClaimedByAsync(claim.DonationId, "requested", claim.CenterUserId);
            }

            // Fetch updated claim
            var updated = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new InvalidOperationException("Failed to retrieve updated claim.");

            var centerUser = await _userRepository.GetByIdAsync(updated.CenterUserId);
            return MapToResponse(updated, centerUser);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Get all claims for the donator's donations
        // ───────────────────────────────────────────────────────────────────────
        public async Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByDonatorAsync(int donatorUserId)
        {
            var claims = await _claimRepository.GetByDonatorUserIdAsync(donatorUserId);
            var results = new List<DonationClaimResponseDto>();

            foreach (var claim in claims)
            {
                var centerUser = await _userRepository.GetByIdAsync(claim.CenterUserId);
                results.Add(MapToResponse(claim, centerUser));
            }

            return results;
        }

        // ───────────────────────────────────────────────────────────────────────
        // Get all claims created by a distribution center
        // ───────────────────────────────────────────────────────────────────────
        public async Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByCenterAsync(int centerUserId)
        {
            var claims = await _claimRepository.GetByCenterUserIdAsync(centerUserId);
            var centerUser = await _userRepository.GetByIdAsync(centerUserId);
            
            return claims.Select(c => MapToResponse(c, centerUser)).ToList();
        }

        // ── Mapping ────────────────────────────────────────────────────────────
        private static DonationClaimResponseDto MapToResponse(DonationClaim claim, User? centerUser = null)
        {
            return new DonationClaimResponseDto
            {
                ClaimId = claim.ClaimId,
                DonationId = claim.DonationId,
                CenterUserId = claim.CenterUserId,
                DonatorUserId = claim.DonatorUserId,
                Status = claim.Status,
                CreatedAt = claim.CreatedAt,
                Center = centerUser == null ? null : new UserProfileDto
                {
                    UserId = centerUser.UserId,
                    Name = centerUser.Name,
                    Email = centerUser.Email,
                    Role = centerUser.UserType,
                    PhoneNumber = string.IsNullOrWhiteSpace(centerUser.PhoneNumber) ? null : centerUser.PhoneNumber,
                    Address = string.IsNullOrWhiteSpace(centerUser.Address) ? null : centerUser.Address,
                    CreatedAt = centerUser.CreatedAt
                }
            };
        }
    }
}
