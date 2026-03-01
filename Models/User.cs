namespace RestroPlate.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // DONOR | DISTRIBUTION_CENTER
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
