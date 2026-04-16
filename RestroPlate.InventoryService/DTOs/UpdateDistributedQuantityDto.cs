using System.ComponentModel.DataAnnotations;

namespace RestroPlate.InventoryService.DTOs
{
    public class UpdateDistributedQuantityDto
    {
        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Distributed quantity must be greater than zero.")]
        public decimal DistributedQuantity { get; set; }
    }
}