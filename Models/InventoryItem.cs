using System;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Category { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal CurrentStock { get; set; }

        [Required]
        public string Unit { get; set; } // kg, g, L, pieces

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Threshold { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal CostPerUnit { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}