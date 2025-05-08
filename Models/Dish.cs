using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public class Dish
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string Category { get; set; }

        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int PopularityScore { get; set; } = 0;

        public bool IsSpecialToday { get; set; } = false;

        // Navigation property for ingredients
        public virtual ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();
    }
}
