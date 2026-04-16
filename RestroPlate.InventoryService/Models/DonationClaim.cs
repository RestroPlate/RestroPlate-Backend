namespace RestroPlate.InventoryService.Models
{
    public class DonationClaim
    {
        public int ClaimId { get; set; }
        public int DonationId { get; set; }
        public int CenterUserId { get; set; }
        public int DonatorUserId { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
    }
}