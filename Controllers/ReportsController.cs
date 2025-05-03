using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

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

        // New methods for sales analytics
        [HttpGet]
        public async Task<IActionResult> GetSalesAnalytics(string period = "daily")
        {
            DateTime startDate, endDate = DateTime.Today;

            switch (period.ToLower())
            {
                case "weekly":
                    startDate = endDate.AddDays(-7);
                    break;
                case "monthly":
                    startDate = endDate.AddMonths(-1);
                    break;
                default: // daily
                    startDate = endDate.AddDays(-1);
                    break;
            }

            var salesData = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return Json(salesData);
        }

        [HttpGet]
        public async Task<IActionResult> GetDishOrderCounts(string period = "daily")
        {
            DateTime startDate, endDate = DateTime.Today;

            switch (period.ToLower())
            {
                case "weekly":
                    startDate = endDate.AddDays(-7);
                    break;
                case "monthly":
                    startDate = endDate.AddMonths(-1);
                    break;
                default: // daily
                    startDate = endDate.AddDays(-1);
                    break;
            }

            var dishOrderCounts = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .GroupBy(oi => oi.Dish.Name)
                .Select(g => new
                {
                    DishName = g.Key,
                    OrderCount = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(d => d.OrderCount)
                .ToListAsync();

            return Json(dishOrderCounts);
        }
    }
}