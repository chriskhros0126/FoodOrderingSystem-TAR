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
            // Redirect to DishPopularity instead
            return RedirectToAction("DishPopularity");
        }

        public async Task<IActionResult> DishPopularity()
        {
            // Get dishes with their order counts
            var dishesWithOrders = await _context.Dishes
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Category,
                    d.PopularityScore,
                    OrderCount = _context.OrderItems.Where(oi => oi.DishId == d.Id).Sum(oi => oi.Quantity)
                })
                .OrderByDescending(d => d.OrderCount)
                .ToListAsync();

            // Pass the data to the view
            ViewBag.DishesWithOrders = dishesWithOrders;
            
            return View();
        }

        public IActionResult SalesAnalytics()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDetailedSalesAnalytics(string period = "daily", string groupBy = "date")
        {
            // Define date range based on period
            DateTime fromDate = period switch
            {
                "weekly" => DateTime.Today.AddDays(-7),
                "monthly" => DateTime.Today.AddDays(-30),
                "yearly" => DateTime.Today.AddDays(-365),
                _ => DateTime.Today.AddDays(-1) // default: daily (last 24 hours)
            };
            
            // Base query to get orders within the specified period
            var ordersQuery = _context.Orders
                .Where(o => o.OrderDate >= fromDate)
                .AsQueryable();
                
            // Group and aggregate data based on the specified grouping
            if (groupBy == "category")
            {
                // Group by dish category
                var categorySales = await _context.OrderItems
                    .Where(oi => oi.Order.OrderDate >= fromDate)
                    .Join(_context.Dishes, oi => oi.DishId, d => d.Id, (oi, d) => new { OrderItem = oi, Dish = d })
                    .GroupBy(x => x.Dish.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        OrderCount = g.Count(),
                        TotalSales = g.Sum(x => x.OrderItem.Quantity * x.OrderItem.UnitPrice),
                        ItemCount = g.Sum(x => x.OrderItem.Quantity)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToListAsync();
                    
                return Json(categorySales);
            }
            else if (groupBy == "payment")
            {
                // Group by payment method
                var paymentSales = await _context.Payments
                    .Where(p => p.PaymentDate >= fromDate)
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new
                    {
                        PaymentMethod = g.Key,
                        OrderCount = g.Count(),
                        TotalSales = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToListAsync();
                    
                return Json(paymentSales);
            }
            else
            {
                // Default: Group by date
                var dateFormat = period switch
                {
                    "yearly" => "yyyy-MM",     // Year-Month for yearly
                    "monthly" => "MM-dd",      // Month-Day for monthly
                    _ => "HH:00"               // Hour for daily/weekly
                };
                
                var dateSales = await ordersQuery
                    .GroupBy(o => new 
                    { 
                        Date = period == "yearly" 
                            ? new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1) 
                            : period == "monthly" 
                                ? new DateTime(DateTime.Now.Year, o.OrderDate.Month, o.OrderDate.Day) 
                                : new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, o.OrderDate.Hour, 0, 0)
                    })
                    .Select(g => new
                    {
                        Date = g.Key.Date,
                        DateLabel = g.Key.Date.ToString(dateFormat),
                        OrderCount = g.Count(),
                        TotalSales = g.Sum(o => o.TotalAmount),
                        AverageOrderValue = g.Average(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();
                    
                return Json(dateSales);
            }
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

        [HttpGet]
        public async Task<IActionResult> GetDishPopularityData()
        {
            var dishPopularity = await _context.Dishes
                .Select(d => new
                {
                    Name = d.Name,
                    PopularityScore = d.PopularityScore,
                    Category = d.Category,
                    OrderCount = _context.OrderItems.Where(oi => oi.DishId == d.Id).Sum(oi => oi.Quantity)
                })
                .Where(d => d.OrderCount > 0 || d.PopularityScore > 0)
                .OrderByDescending(d => d.OrderCount)
                .ToListAsync();

            return Json(dishPopularity);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesAnalytics(string period)
        {
            DateTime fromDate = period switch
            {
                "weekly" => DateTime.Today.AddDays(-7),
                "monthly" => DateTime.Today.AddDays(-30),
                _ => DateTime.Today // default: daily
            };

            var sales = await _context.Orders
                .Where(o => o.OrderDate >= fromDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Json(sales);
        }

        [HttpGet]
        public async Task<IActionResult> GetDishOrderCounts(string period)
        {
            DateTime fromDate = period switch
            {
                "weekly" => DateTime.Today.AddDays(-7),
                "monthly" => DateTime.Today.AddDays(-30),
                _ => DateTime.Today
            };

            var result = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= fromDate)
                .GroupBy(oi => oi.Dish.Name)
                .Select(g => new
                {
                    DishName = g.Key,
                    OrderCount = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(g => g.OrderCount)
                .ToListAsync();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SeedDemoData()
        {
            // Only seed if there are no orders
            if (!_context.Orders.Any())
            {
                // Add demo dishes
                var dishes = new List<Dish>
                {
                    new Dish { Name = "Spaghetti", Category = "Main", Price = 12.99m, IsAvailable = true, PopularityScore = 80 },
                    new Dish { Name = "Caesar Salad", Category = "Starter", Price = 6.99m, IsAvailable = true, PopularityScore = 60 },
                    new Dish { Name = "Cheesecake", Category = "Dessert", Price = 5.99m, IsAvailable = true, PopularityScore = 70 },
                    new Dish { Name = "Soup", Category = "Starter", Price = 4.99m, IsAvailable = false, PopularityScore = 40 }
                };
                _context.Dishes.AddRange(dishes);
                await _context.SaveChangesAsync();

                // Add demo orders
                var order1 = new Order
                {
                    CustomerName = "Alice",
                    CustomerPhone = "123456789",
                    DeliveryAddress = "123 Main St",
                    OrderDate = DateTime.Today.AddDays(-1),
                    Status = OrderStatus.Delivered,
                    TotalAmount = 25.98m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { DishId = dishes[0].Id, Quantity = 1, UnitPrice = dishes[0].Price },
                        new OrderItem { DishId = dishes[1].Id, Quantity = 1, UnitPrice = dishes[1].Price }
                    }
                };
                var order2 = new Order
                {
                    CustomerName = "Bob",
                    CustomerPhone = "987654321",
                    DeliveryAddress = "456 Side St",
                    OrderDate = DateTime.Today,
                    Status = OrderStatus.Confirmed,
                    TotalAmount = 11.98m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { DishId = dishes[2].Id, Quantity = 2, UnitPrice = dishes[2].Price }
                    }
                };
                _context.Orders.AddRange(order1, order2);
                await _context.SaveChangesAsync();
            }

            return Content("Demo data seeded! You can now view charts on the Reports page.");
        }
    }
}