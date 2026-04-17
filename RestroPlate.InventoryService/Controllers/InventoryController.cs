using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.InventoryService.DTOs;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize(Roles = "DISTRIBUTION_CENTER")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<InventoryLogResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInventory()
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            var inventory = await _inventoryService.GetInventoryAsync(userId.Value);
            return Ok(inventory);
        }

        [HttpPost("{id:int}/collect")]
        [ProducesResponseType(typeof(InventoryLogResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
                var inventoryLog = await _inventoryService.CollectDonationAsync(id, userId.Value, request);
                return Ok(inventoryLog);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

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
                var centerName = User.FindFirstValue("center_name")
                                ?? User.FindFirstValue(ClaimTypes.Name)
                                ?? $"Center {userId.Value}";

                var centerAddress = User.FindFirstValue("center_address")
                                   ?? User.FindFirstValue("address")
                                   ?? "N/A";

                await _inventoryService.UpdateInventoryPublishStatusAsync(id, userId.Value, isPublished, centerName, centerAddress);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("published")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<InventoryLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedInventory()
        {
            var publishedInventory = await _inventoryService.GetPublishedInventoryAsync();
            return Ok(publishedInventory);
        }

        [HttpPatch("logs/{id:int}/distribute")]
        [ProducesResponseType(typeof(InventoryLogResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DistributeInventory(int id, [FromBody] UpdateDistributedQuantityDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var updatedLog = await _inventoryService.UpdateDistributedQuantityAsync(id, userId.Value, request.DistributedQuantity);
                return Ok(updatedLog);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
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