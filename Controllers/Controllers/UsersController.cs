using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Security.Claims;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Get the authenticated user's profile. Requires a valid JWT Bearer token.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            // Extract userId from the JWT sub claim
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            if (subClaim is null || !int.TryParse(subClaim, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var profile = await _authService.GetProfileAsync(userId);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
