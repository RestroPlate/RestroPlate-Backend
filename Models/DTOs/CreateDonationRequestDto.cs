namespace RestroPlate.Models.DTOs
{
    public class CreateDonationRequestDto
    {
        public string FoodType { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
