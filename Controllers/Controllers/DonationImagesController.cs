using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/donations/{donationId:int}/images")]
    [Authorize]
    public class DonationImagesController : ControllerBase
    {
        private readonly IDonationImageService _imageService;

        public DonationImagesController(IDonationImageService imageService)
        {
            _imageService = imageService;
        }

        /// <summary>
        /// Donor uploads an image for a donation.
        /// POST /api/donations/{donationId}/images
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(typeof(DonationImageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UploadImage(int donationId, IFormFile file)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var image = await _imageService.UploadImageAsync(
                    donationId,
                    userId.Value,
                    file.OpenReadStream(),
                    file.FileName,
                    file.ContentType,
                    file.Length);
                return StatusCode(StatusCodes.Status201Created, image);
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

        /// <summary>
        /// Donor or Distribution Center views images for a donation.
        /// GET /api/donations/{donationId}/images
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "DONOR,DISTRIBUTION_CENTER")]
        [ProducesResponseType(typeof(IReadOnlyList<DonationImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetImages(int donationId)
        {
            var images = await _imageService.GetImagesAsync(donationId);
            return Ok(images);
        }

        /// <summary>
        /// Donor deletes a specific image from their donation.
        /// DELETE /api/donations/{donationId}/images/{imageId}
        /// </summary>
        [HttpDelete("{imageId:int}")]
        [Authorize(Roles = "DONOR")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteImage(int donationId, int imageId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var deleted = await _imageService.DeleteImageAsync(imageId, donationId, userId.Value);
                if (!deleted)
                    return NotFound(new { message = "Image not found." });

                return NoContent();
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

        private int? GetAuthenticatedUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");
            return int.TryParse(subClaim, out var userId) ? userId : null;
        }
    }
}
