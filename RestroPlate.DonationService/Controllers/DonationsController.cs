using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.DonationService.Models.DTOs;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Controllers
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

        [HttpPost]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateDonation([FromBody] CreateDonationDto request)
        {
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
                return Conflict(new { message = ex.Message });
            }
        }

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
                return Conflict(new { message = ex.Message });
            }
        }

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
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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
                return Conflict(new { message = ex.Message });
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