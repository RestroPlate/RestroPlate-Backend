using System.ComponentModel.DataAnnotations;

namespace RestroPlate.InventoryService.DTOs
{
    public class CollectDonationDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "CollectedAmount must be greater than zero.")]
        public decimal CollectedAmount { get; set; }
    }
}