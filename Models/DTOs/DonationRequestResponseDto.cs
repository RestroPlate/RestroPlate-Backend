namespace RestroPlate.Models.DTOs
{
    public class DonationRequestResponseDto
    {
        public int DonationRequestId { get; set; }
        public int DistributionCenterUserId { get; set; }
        public string DistributionCenterName { get; set; } = string.Empty;
        public string DistributionCenterAddress { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal DonatedQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string FoodType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
