namespace RestroPlate.Models.DTOs
{
    public class CreateDonationRequestRequestDto
    {
        public int DonationId { get; set; }
        public decimal RequestedQuantity { get; set; }
    }
}
