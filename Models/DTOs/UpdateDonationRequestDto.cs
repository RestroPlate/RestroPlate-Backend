namespace RestroPlate.Models.DTOs
{
    public class UpdateDonationRequestDto
    {
        public string FoodType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string AvailabilityTime { get; set; } = string.Empty;
    }
}
