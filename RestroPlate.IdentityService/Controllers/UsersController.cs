using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.IdentityService.DTOs;
using RestroPlate.IdentityService.Models.Interfaces;

namespace RestroPlate.IdentityService.Controllers
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

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
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