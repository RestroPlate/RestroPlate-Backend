using RestroPlate.InventoryService.DTOs;
using RestroPlate.InventoryService.Models;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Services
{
    public class DonationClaimService : IDonationClaimService
    {
        private readonly IDonationClaimRepository _claimRepository;

        public DonationClaimService(IDonationClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
        }

        public async Task<DonationClaimResponseDto> CreateClaimAsync(int centerUserId, CreateDonationClaimDto request)
        {
            if (request.DonationId <= 0)
                throw new ArgumentException("DonationId must be greater than zero.");

            if (request.DonatorUserId <= 0)
                throw new ArgumentException("DonatorUserId must be greater than zero.");

            var claim = new DonationClaim
            {
                DonationId = request.DonationId,
                CenterUserId = centerUserId,
                DonatorUserId = request.DonatorUserId,
                Status = "pending"
            };

            var claimId = await _claimRepository.CreateAsync(claim);
            var created = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new InvalidOperationException("Failed to retrieve created claim.");

            return MapToResponse(created);
        }

        public async Task<DonationClaimResponseDto> UpdateClaimStatusAsync(int claimId, int donatorUserId, UpdateDonationClaimStatusDto request)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new KeyNotFoundException($"Claim {claimId} not found.");

            if (claim.DonatorUserId != donatorUserId)
                throw new KeyNotFoundException($"Claim {claimId} not found.");

            if (!string.Equals(claim.Status, "pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Claim {claimId} is already {claim.Status}. Only pending claims can be updated.");

            var normalizedStatus = request.Status?.Trim().ToLowerInvariant();
            if (normalizedStatus != "accepted" && normalizedStatus != "rejected")
                throw new ArgumentException("Status must be 'accepted' or 'rejected'.");

            await _claimRepository.UpdateStatusAsync(claimId, normalizedStatus);

            var updated = await _claimRepository.GetByIdAsync(claimId)
                ?? throw new InvalidOperationException("Failed to retrieve updated claim.");

            return MapToResponse(updated);
        }

        public async Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByDonatorAsync(int donatorUserId)
        {
            var claims = await _claimRepository.GetByDonatorUserIdAsync(donatorUserId);
            return claims.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<DonationClaimResponseDto>> GetClaimsByCenterAsync(int centerUserId)
        {
            var claims = await _claimRepository.GetByCenterUserIdAsync(centerUserId);
            return claims.Select(MapToResponse).ToList();
        }

        private static DonationClaimResponseDto MapToResponse(DonationClaim claim)
        {
            return new DonationClaimResponseDto
            {
                ClaimId = claim.ClaimId,
                DonationId = claim.DonationId,
                CenterUserId = claim.CenterUserId,
                DonatorUserId = claim.DonatorUserId,
                Status = claim.Status,
                CreatedAt = claim.CreatedAt
            };
        }
    }
}