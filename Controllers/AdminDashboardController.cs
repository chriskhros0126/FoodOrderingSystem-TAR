using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using FoodOrderingSystem.Attributes;

namespace FoodOrderingSystem.Controllers;

[Authorize(Roles = "Admin")]
[AdminLayout]
public class AdminDashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(AppDbContext context, ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Dashboard metrics
        ViewBag.TotalOrders = await _context.Orders.CountAsync();
        ViewBag.PendingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Confirmed).CountAsync();
        
        // Financial stats
        var totalRevenue = await _context.Payments.SumAsync(p => p.Amount);
        ViewBag.TotalRevenue = totalRevenue;
        
        // Calculate revenue this month
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        ViewBag.MonthlyRevenue = await _context.Payments
            .Where(p => p.PaymentDate >= startOfMonth && p.PaymentDate <= endOfMonth)
            .SumAsync(p => p.Amount);
        
        // Inventory stats
        ViewBag.MenuItemsCount = await _context.Dishes.CountAsync();
        ViewBag.LowStockItems = await _context.InventoryItems.Where(i => i.CurrentStock <= i.Threshold).CountAsync();
        
        // Reservations
        ViewBag.ReservationsToday = await _context.TableReservations
            .Where(r => r.ReservationDate.Date == DateTime.Today.Date)
            .CountAsync();
        
        // Recent orders
        ViewBag.RecentOrders = await _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();

        // Recent reservations
        ViewBag.RecentReservations = await _context.TableReservations
            .OrderByDescending(r => r.ReservationDate)
            .Take(5)
            .ToListAsync();
            
        // Sales trend data for chart
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-i))
            .Reverse()
            .ToList();
            
        var salesData = new List<object>();
        foreach (var day in last7Days)
        {
            var dailySales = await _context.Orders
                .Where(o => o.OrderDate.Date == day.Date)
                .SumAsync(o => o.TotalAmount);
                
            salesData.Add(new 
            { 
                Day = day.ToString("MM/dd"), 
                Sales = dailySales 
            });
        }
        
        ViewBag.SalesData = salesData;
        
        return View();
    }
} 