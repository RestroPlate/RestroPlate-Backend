using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize(Roles = "DISTRIBUTION_CENTER")]
    public class InventoryController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public InventoryController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        /// <summary>
        /// Retrieves the claimed donations (inventory) for the authenticated Distribution Center.
        /// Filters by status 'requested' and 'collected', ordered by newest first.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<DonationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInventory()
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            var inventory = await _donationService.GetCenterInventoryAsync(userId.Value);
            return Ok(inventory);
        }

        /// <summary>
        /// DC collects a requested donation.
        /// Transitions status: requested → collected.
        /// Adds a log entry to DC inventory_logs with collected amount.
        /// </summary>
        [HttpPost("{id:int}/collect")]
        [ProducesResponseType(typeof(InventoryLogResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CollectDonation(int id, [FromBody] CollectDonationDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var inventoryLog = await _donationService.CollectDonationAsync(id, userId.Value, request);
                return Ok(inventoryLog);
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
                return Conflict(new { message = ex.Message }); // 409 for invalid status transition
            }
        }

        /// <summary>
        /// DC toggles whether their collected inventory is published to the community.
        /// </summary>
        [HttpPatch("{id:int}/publish")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PublishInventory(int id, [FromBody] bool isPublished)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                await _donationService.UpdateInventoryPublishStatusAsync(id, userId.Value, isPublished);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 if trying to publish someone else's log
            }
        }

        /// <summary>
        /// Public endpoint to view all published inventory by Distribution Centers.
        /// </summary>
        [HttpGet("published")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<InventoryLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedInventory()
        {
            var publishedInventory = await _donationService.GetPublishedInventoryAsync();
            return Ok(publishedInventory);
        }

        private int? GetAuthenticatedUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            return int.TryParse(subClaim, out var userId) ? userId : null;
        }
    }
}
