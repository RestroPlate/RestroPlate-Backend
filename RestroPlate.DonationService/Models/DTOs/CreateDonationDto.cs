namespace RestroPlate.DonationService.Models.DTOs
{
    public class CreateDonationDto
    {
        public int? DonationRequestId { get; set; }
        public string FoodType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string AvailabilityTime { get; set; } = string.Empty;
    }
}