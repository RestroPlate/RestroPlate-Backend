namespace RestroPlate.Models
{
    // new — InventoryLog tracks every collect action performed by a DC
    public class InventoryLog
    {
        public int InventoryLogId { get; set; }
        public int DonationId { get; set; }
        public int? DonationRequestId { get; set; }
        public int DistributionCenterUserId { get; set; }
        public decimal CollectedAmount { get; set; }
        public DateTime CollectedAt { get; set; }
    }
}
