namespace RestroPlate.Models.DTOs
{
    // new — response for the collect endpoint; returns the created inventory log entry
    public class InventoryLogResponseDto
    {
        public int InventoryLogId { get; set; }
        public int DonationId { get; set; }
        public int? DonationRequestId { get; set; }
        public int DistributionCenterUserId { get; set; }
        public decimal CollectedAmount { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CollectedAt { get; set; }
    }
}
