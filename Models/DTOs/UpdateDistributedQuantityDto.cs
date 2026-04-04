using System.ComponentModel.DataAnnotations;

namespace RestroPlate.Models.DTOs
{
    public class UpdateDistributedQuantityDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Distributed quantity must be greater than zero.")]
        public decimal DistributedQuantity { get; set; }
    }
}
