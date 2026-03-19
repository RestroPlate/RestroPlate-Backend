using System.ComponentModel.DataAnnotations;

namespace RestroPlate.Models.DTOs
{
    public class UpdateCollectedQuantityDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Collected quantity must be greater than zero.")]
        public decimal CollectedQuantity { get; set; }
    }
}
