namespace RestroPlate.Models.DTOs
{
    public class DistributionInventoryResponseDto
    {
        public int InventoryId { get; set; }
        public int DonationRequestId { get; set; }
        public decimal CollectedQuantity { get; set; }
        public DateTime CollectionDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
