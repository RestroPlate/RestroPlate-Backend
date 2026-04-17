namespace RestroPlate.PublicService.DTOs;

public class PublishedDonationDto
{
    public int DonationId { get; set; }
    public string FoodType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public DateTime CollectedAt { get; set; }
}