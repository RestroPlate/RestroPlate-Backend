using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous]
    public class PublicController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public PublicController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        /// <summary>
        /// Fetches all distribution centers along with their published donation details.
        /// GET /api/public/centers-with-donations
        /// </summary>
        [HttpGet("centers-with-donations")]
        [ProducesResponseType(typeof(IEnumerable<CenterWithDonationsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCentersWithDonations()
        {
            var centers = await _donationService.GetPublicCentersWithDonationsAsync();
            return Ok(centers);
        }
    }
}
