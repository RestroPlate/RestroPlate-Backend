namespace RestroPlate.Models.DTOs
{
    public class DonationClaimResponseDto
    {
        public int ClaimId { get; set; }
        public int DonationId { get; set; }
        public int CenterUserId { get; set; }
        public int DonatorUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
