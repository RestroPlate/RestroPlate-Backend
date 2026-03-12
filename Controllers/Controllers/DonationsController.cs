using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/donations")]
        [Authorize(Roles = "DONOR")]
    public class DonationsController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationsController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateDonation([FromBody] CreateDonationRequestDto request)
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
        }

        [HttpGet]
        [HttpGet("me")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyDonations()
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            var donations = await _donationService.GetUserDonationsAsync(userId.Value);
            return Ok(donations);
        }

        private int? GetAuthenticatedUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            return int.TryParse(subClaim, out var userId) ? userId : null;
        }
    }
}
