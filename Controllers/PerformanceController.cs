using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;

namespace FoodOrderingSystem.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly AppDbContext _context;

        public PerformanceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var riders = await _context.Riders
                .Include(r => r.Orders)
                .ToListAsync();

            return View(riders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rider = await _context.Riders
                .Include(r => r.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rider == null)
            {
                return NotFound();
            }

            // Calculate performance metrics
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var performanceMetrics = new RiderPerformanceViewModel
            {
                Rider = rider,
                TodayDeliveries = rider.Orders.Count(o => o.OrderDate.Date == today),
                WeekDeliveries = rider.Orders.Count(o => o.OrderDate >= thisWeek),
                MonthDeliveries = rider.Orders.Count(o => o.OrderDate >= thisMonth),
                AverageDeliveryTime = CalculateAverageDeliveryTime(rider.Orders),
                OnTimeDeliveryRate = CalculateOnTimeDeliveryRate(rider.Orders),
                RecentDeliveries = rider.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToList()
            };

            return View(performanceMetrics);
        }

        private TimeSpan CalculateAverageDeliveryTime(ICollection<Order> orders)
        {
            var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
            if (!deliveredOrders.Any()) return TimeSpan.Zero;

            var totalTime = deliveredOrders.Sum(o => 
                (o.OrderDate - o.OrderDate).TotalMinutes); // Replace with actual delivery time calculation
            return TimeSpan.FromMinutes(totalTime / deliveredOrders.Count);
        }

        private double CalculateOnTimeDeliveryRate(ICollection<Order> orders)
        {
            var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
            if (!deliveredOrders.Any()) return 0;

            var onTimeDeliveries = deliveredOrders.Count(o => 
                (o.OrderDate - o.OrderDate).TotalMinutes <= 30); // Replace with actual on-time threshold
            return (double)onTimeDeliveries / deliveredOrders.Count * 100;
        }
    }

    public class RiderPerformanceViewModel
    {
        public Rider Rider { get; set; }
        public int TodayDeliveries { get; set; }
        public int WeekDeliveries { get; set; }
        public int MonthDeliveries { get; set; }
        public TimeSpan AverageDeliveryTime { get; set; }
        public double OnTimeDeliveryRate { get; set; }
        public List<Order> RecentDeliveries { get; set; }
    }
} 