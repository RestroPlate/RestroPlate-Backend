using System.ComponentModel.DataAnnotations;

namespace RestroPlate.Models.DTOs
{
    // new — payload for PATCH /donations/{id}/collect
    public class CollectDonationDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "CollectedAmount must be greater than zero.")]
        public decimal CollectedAmount { get; set; }
    }
}
