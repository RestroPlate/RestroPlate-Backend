using Microsoft.AspNetCore.Mvc;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Controllers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        // Inject the Repository
        public TestController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("db-check")]
        public IActionResult CheckDatabase()
        {
            var isConnected = _userRepository.TestConnection();

            if (isConnected)
                return Ok("Database Connection Successful!");

            return StatusCode(500, "Database Connection Failed.");
        }
    }
}