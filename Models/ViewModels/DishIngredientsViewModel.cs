using System.Collections.Generic;
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Models.ViewModels
{
    public class DishIngredientsViewModel
    {
        public int DishId { get; set; }
        public string DishName { get; set; }
        public List<DishIngredient> CurrentIngredients { get; set; }
        public List<InventoryItem> AvailableIngredients { get; set; }
    }

    public class DishIngredientViewModel
    {
        public int DishId { get; set; }
        public int InventoryItemId { get; set; }
        public decimal QuantityRequired { get; set; }
    }
}