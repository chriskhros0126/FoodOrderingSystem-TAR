using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Dish
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        [Range(1, 500)]
        public decimal Price { get; set; }

        public string? Category { get; set; } // e.g., "Starter", "Main", "Dessert"

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsSpecialToday { get; set; }

        public int PopularityScore { get; set; } // used in reports
    }
}
