using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/donation-requests")]
    [Authorize]
    public class DonationRequestsController : ControllerBase
    {
        private readonly IDonationRequestService _donationRequestService;

        public DonationRequestsController(IDonationRequestService donationRequestService)
        {
            _donationRequestService = donationRequestService;
        }

        [HttpPost]
        [Authorize(Roles = "DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(DonationRequestResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDonationRequest([FromBody] CreateDonationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var createdRequest = await _donationRequestService.CreateDonationRequestAsync(userId.Value, request);
                return StatusCode(StatusCodes.Status201Created, createdRequest);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{donationRequestId}/quantity")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationRequestResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRequestQuantity(int donationRequestId, [FromBody] UpdateDonationRequestQuantityDto request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var updatedRequest = await _donationRequestService.UpdateDonationRequestQuantityAsync(donationRequestId, request.DonatedQuantity);
                return Ok(updatedRequest);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("available")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationRequestResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAvailableRequests([FromQuery] string? status = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var requests = await _donationRequestService.GetAvailableRequestsAsync(status);
                return Ok(requests);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("outgoing")]
        [Authorize(Roles = "DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationRequestResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOutgoingRequests([FromQuery] string? status = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var requests = await _donationRequestService.GetOutgoingRequestsAsync(userId.Value, status);
                return Ok(requests);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
