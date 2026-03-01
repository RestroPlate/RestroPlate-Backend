namespace RestroPlate.Models.DTOs
{
    public class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Must be either "DONOR" or "DISTRIBUTION_CENTER"
        /// </summary>
        public string Role { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }
}
