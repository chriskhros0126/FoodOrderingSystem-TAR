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

    public async Task<IActionResult> Index()
    {
        // Get featured dishes (special today or popular)
        var featuredDishes = await _context.Dishes
            .Where(d => d.IsAvailable && (d.IsSpecialToday || d.PopularityScore > 0))
            .OrderByDescending(d => d.IsSpecialToday)
            .ThenByDescending(d => d.PopularityScore)
            .Take(6)
            .ToListAsync();

        ViewBag.FeaturedDishes = featuredDishes;
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
