namespace RestroPlate.Models
{
    public class DistributionInventory
    {
        public int InventoryId { get; set; }
        public int DonationRequestId { get; set; }
        public decimal CollectedQuantity { get; set; }
        public DateTime CollectionDate { get; set; }
    }
}
