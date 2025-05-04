using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public DeliveryController(AppDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Delivery/Index
        public async Task<IActionResult> Index()
        {
            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .OrderByDescending(d => d.AssignedTime)
                .ToListAsync();

            return View(deliveries);
        }

        // GET: Delivery/AssignRider
        public async Task<IActionResult> AssignRider()
        {
            // Get orders with Preparing status or OutForDelivery status that don't have any delivery assigned
            var ordersToAssign = await _context.Orders
                .Where(o => (o.Status == OrderStatus.Preparing || o.Status == OrderStatus.OutForDelivery) 
                       && !_context.Deliveries.Any(d => d.OrderId == o.Id))
                .ToListAsync();

            // Get available riders
            var availableRiders = await _context.Riders
                .Where(r => r.IsAvailable)
                .ToListAsync();

            // Prepare view model
            var viewModel = new AssignRiderViewModel
            {
                Orders = ordersToAssign,
                Riders = availableRiders,
                AvailableOrdersSelectList = new SelectList(ordersToAssign, "Id", "Id"),
                AvailableRidersSelectList = new SelectList(availableRiders, "Id", "Name")
            };

            return View(viewModel);
        }

        // POST: Delivery/AssignRider
        [HttpPost]
        public async Task<IActionResult> AssignRider(AssignRiderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var order = await _context.Orders.FindAsync(model.OrderId);
                var rider = await _context.Riders.FindAsync(model.RiderId);

                if (order == null || rider == null)
                {
                    ModelState.AddModelError("", "Order or Rider not found.");
                    return View(model);
                }

                if (order.Status != OrderStatus.Preparing && order.Status != OrderStatus.OutForDelivery)
                {
                    ModelState.AddModelError("", "Order is not ready for delivery.");
                    return View(model);
                }

                if (!rider.IsAvailable)
                {
                    ModelState.AddModelError("", "Rider is not available.");
                    return View(model);
                }

                // Update order status to OutForDelivery if it's currently Preparing
                if (order.Status == OrderStatus.Preparing)
                {
                    order.Status = OrderStatus.OutForDelivery;
                }

                // Create new delivery
                var delivery = new Delivery
                {
                    OrderId = model.OrderId,
                    RiderId = model.RiderId,
                    AssignedTime = DateTime.Now,
                    Status = DeliveryStatus.Assigned,
                    Notes = model.Notes
                };

                // Update rider information
                rider.IsAvailable = false;
                rider.LastActive = DateTime.Now;

                _context.Deliveries.Add(delivery);
                await _context.SaveChangesAsync();

                // Notify connected clients
                await _hubContext.Clients.All.SendAsync("DeliveryAssigned", delivery.Id);

                TempData["SuccessMessage"] = $"Order #{order.Id} has been assigned to {rider.Name}";
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            var ordersToAssign = await _context.Orders
                .Where(o => (o.Status == OrderStatus.Preparing || o.Status == OrderStatus.OutForDelivery) 
                       && !_context.Deliveries.Any(d => d.OrderId == o.Id))
                .ToListAsync();

            var availableRiders = await _context.Riders
                .Where(r => r.IsAvailable)
                .ToListAsync();

            model.Orders = ordersToAssign;
            model.Riders = availableRiders;
            model.AvailableOrdersSelectList = new SelectList(ordersToAssign, "Id", "Id");
            model.AvailableRidersSelectList = new SelectList(availableRiders, "Id", "Name");

            return View(model);
        }

        // GET: Delivery/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var delivery = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            return View(delivery);
        }

        // GET: Delivery/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var delivery = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(DeliveryStatus)).Cast<DeliveryStatus>(), delivery.Status);
            return View(delivery);
        }

        // POST: Delivery/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, DeliveryStatus status)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            delivery.Status = status;

            // Update timestamps based on status
            if (status == DeliveryStatus.PickedUp && !delivery.PickupTime.HasValue)
            {
                delivery.PickupTime = DateTime.Now;
            }
            else if (status == DeliveryStatus.Delivered && !delivery.DeliveryTime.HasValue)
            {
                delivery.DeliveryTime = DateTime.Now;
                delivery.Rider.IsAvailable = true;
                delivery.Rider.TotalDeliveries++;
                delivery.Order.Status = OrderStatus.Delivered;
            }
            else if (status == DeliveryStatus.Failed)
            {
                delivery.Rider.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            // Notify connected clients
            await _hubContext.Clients.All.SendAsync("DeliveryStatusUpdated", delivery.Id, status);

            TempData["SuccessMessage"] = "Delivery status updated successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Delivery/RiderDeliveries/5
        public async Task<IActionResult> RiderDeliveries(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rider = await _context.Riders
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rider == null)
            {
                return NotFound();
            }

            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Where(d => d.RiderId == id)
                .OrderByDescending(d => d.AssignedTime)
                .ToListAsync();

            ViewBag.Rider = rider;
            return View(deliveries);
        }

        // GET: Delivery/ActiveDeliveries
        public async Task<IActionResult> ActiveDeliveries()
        {
            var activeDeliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .Where(d => d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Failed)
                .OrderByDescending(d => d.AssignedTime)
                .ToListAsync();

            return View(activeDeliveries);
        }
    }

    public class AssignRiderViewModel
    {
        public int OrderId { get; set; }
        public int RiderId { get; set; }
        public string? Notes { get; set; }

        // For display purposes
        public IEnumerable<Order> Orders { get; set; }
        public IEnumerable<Rider> Riders { get; set; }
        public SelectList AvailableOrdersSelectList { get; set; }
        public SelectList AvailableRidersSelectList { get; set; }
    }
}
