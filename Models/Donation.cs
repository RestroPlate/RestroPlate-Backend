namespace RestroPlate.Models
{
    public class Donation
    {
        public int DonationId { get; set; }
        public int? DonationRequestId { get; set; }
        public int ProviderUserId { get; set; }
        public string FoodType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string AvailabilityTime { get; set; } = string.Empty;
        public string Status { get; set; } = "available";
        public int? ClaimedByCenterUserId { get; set; }
        public bool IsPublished { get; set; }
        public int? InventoryLogId { get; set; }
        public decimal? CollectedAmount { get; set; }
        public decimal? DistributedQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
