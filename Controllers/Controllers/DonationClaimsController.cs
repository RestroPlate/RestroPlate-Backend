using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/donation-claims")]
    public class DonationClaimsController : ControllerBase
    {
        private readonly IDonationClaimService _claimService;

        public DonationClaimsController(IDonationClaimService claimService)
        {
            _claimService = claimService;
        }

        /// <summary>
        /// Distribution center creates a claim on an available donation.
        /// POST /api/donation-claims
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(DonationClaimResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateClaim([FromBody] CreateDonationClaimDto request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var claim = await _claimService.CreateClaimAsync(userId.Value, request);
                return CreatedAtAction(nameof(CreateClaim), new { id = claim.ClaimId }, claim);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Donor accepts or rejects a claim on their donation.
        /// PATCH /api/donation-claims/{id}/status
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationClaimResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateClaimStatus(int id, [FromBody] UpdateDonationClaimStatusDto request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var claim = await _claimService.UpdateClaimStatusAsync(id, userId.Value, request);
                
                return Ok(claim);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Donor or Distribution Center views their claims.
        /// Donors see claims on their donations; Centers see claims they created.
        /// GET /api/donation-claims/my
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "DONOR,DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationClaimResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyClaims()
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(role, "DISTRIBUTION_CENTER", StringComparison.OrdinalIgnoreCase))
            {
                var claims = await _claimService.GetClaimsByCenterAsync(userId.Value);
                return Ok(claims);
            }
            else
            {
                var claims = await _claimService.GetClaimsByDonatorAsync(userId.Value);
                return Ok(claims);
            }
        }

        private int? GetAuthenticatedUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            return int.TryParse(subClaim, out var userId) ? userId : null;
        }
    }
}
