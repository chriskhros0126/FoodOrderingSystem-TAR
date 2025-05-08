using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class DishIngredient
    {
        public int Id { get; set; }
        
        [Required]
        public int DishId { get; set; }
        public Dish Dish { get; set; }
        
        [Required]
        public int InventoryItemId { get; set; }
        public InventoryItem InventoryItem { get; set; }
        
        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal QuantityRequired { get; set; }
    }
}