using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data; // Make sure this using directive is included
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context; // ✅ Use the correct name here

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index(string sortOrder, string category)
    {
        // Get all available dishes
        var dishes = _context.Dishes.Where(d => d.IsAvailable);

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            dishes = dishes.Where(d => d.Category == category);
        }

        // Apply sorting
        dishes = sortOrder?.ToLower() switch
        {
            "price_asc" => dishes.OrderBy(d => d.Price),
            "price_desc" => dishes.OrderByDescending(d => d.Price),
            _ => dishes.OrderByDescending(d => d.IsSpecialToday)
            .ThenByDescending(d => d.PopularityScore)
        };

        // Define predefined categories
        ViewBag.Categories = new List<string>
        {
            "Appetizers",
            "Mains",
            "Desserts",
            "Beverages",
            "Sides"
        };

        ViewBag.FeaturedDishes = await dishes.ToListAsync();
        ViewBag.CurrentSort = sortOrder;
        ViewBag.CurrentCategory = category;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
