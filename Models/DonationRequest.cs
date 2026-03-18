namespace RestroPlate.Models
{
    public class DonationRequest
    {
        public int DonationRequestId { get; set; }
        public int DistributionCenterUserId { get; set; }
        public decimal RequestedQuantity { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
        public string FoodType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
