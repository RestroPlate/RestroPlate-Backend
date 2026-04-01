namespace RestroPlate.Models.DTOs
{
    public class UpdateDonationClaimStatusDto
    {
        public string Status { get; set; } = string.Empty; // "accepted" or "rejected"
    }
}
