using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using System.Linq;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dishes = await _context.Dishes
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.PopularityScore,
                    d.IsAvailable,
                    d.Category
                })
                .ToListAsync();

            ViewBag.DishData = dishes;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var dish = await _context.Dishes.FindAsync(id);
            if (dish == null)
            {
                return NotFound();
            }

            dish.IsAvailable = !dish.IsAvailable;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isAvailable = dish.IsAvailable });
        }

        [HttpGet]
        public async Task<IActionResult> GetDishStatistics()
        {
            var statistics = await _context.Dishes
                .GroupBy(d => d.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AvailableCount = g.Count(d => d.IsAvailable),
                    AveragePopularity = g.Average(d => d.PopularityScore)
                })
                .ToListAsync();

            return Json(statistics);
        }
    }
} 