using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/donations")]
    [Authorize]
    public class DonationsController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationsController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        /// <summary>
        /// Flow 1: Donor creates a standalone donation (status = available).
        /// Flow 2: Donor fulfils a pending DC request (DonationRequestId required; status = requested).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateDonation([FromBody] CreateDonationDto request)
        {
            // exists & correct — skipped (validation already in service)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var createdDonation = await _donationService.CreateDonationAsync(userId.Value, request);
                return StatusCode(StatusCodes.Status201Created, createdDonation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409 for wrong status transition
            }
        }

        /// <summary>
        /// Donor views their own donations, filtered by optional status.
        /// </summary>
        // exists & correct — skipped
        [HttpGet]
        [HttpGet("me")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyDonations([FromQuery] string? status = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var donations = await _donationService.GetUserDonationsAsync(userId.Value, status);
                return Ok(donations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Flow 1: DC browses available donations.
        /// GET /api/donations/available
        /// </summary>
        // exists & correct — skipped
        [HttpGet("available")]
        [Authorize(Roles = "DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAvailableDonations(
            [FromQuery] string? location = null,
            [FromQuery] string? foodType = null,
            [FromQuery] string? sortBy = null)
        {
            try
            {
                var donations = await _donationService.GetAvailableDonationsAsync(location, foodType, sortBy);
                return Ok(donations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Flow 1: DC requests an available donation.
        /// Transitions: available → requested. Emits donation.requested event to notify donor.
        /// PATCH /api/donations/{id}/request
        /// </summary>
        // new
        [HttpPatch("{id:int}/request")]
        [Authorize(Roles = "DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RequestDonation(int id)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var donation = await _donationService.RequestDonationAsync(id, userId.Value);
                return Ok(donation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409 for invalid status transition
            }
        }



        /// <summary>
        /// Donor updates an available donation.
        /// </summary>
        // exists & correct — skipped
        [HttpPut("{id:int}")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateDonation(int id, [FromBody] UpdateDonationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var updatedDonation = await _donationService.UpdateDonationAsync(id, userId.Value, request);
                return Ok(updatedDonation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // modified: 409 for wrong status
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Donor deletes an available donation.
        /// </summary>
        // exists & correct — skipped
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                await _donationService.DeleteDonationAsync(id, userId.Value);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // modified: 409 for wrong status
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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
