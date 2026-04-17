namespace RestroPlate.PublicService.Models;

public class PublishedDonationReadModelRow
{
    public int DonationId { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public string CenterAddress { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; }
}