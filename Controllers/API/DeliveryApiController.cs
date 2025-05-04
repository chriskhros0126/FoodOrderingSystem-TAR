using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using FoodOrderingSystem.Hubs;

namespace FoodOrderingSystem.Controllers.API
{
    [Route("api/delivery")]
    [ApiController]
    public class DeliveryApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public DeliveryApiController(AppDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/delivery/rider/{id}
        [HttpGet("rider/{id}")]
        public async Task<ActionResult<IEnumerable<DeliveryDto>>> GetRiderDeliveries(int id)
        {
            var rider = await _context.Riders.FindAsync(id);
            if (rider == null)
            {
                return NotFound();
            }

            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Where(d => d.RiderId == id)
                .OrderByDescending(d => d.AssignedTime)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    OrderId = d.OrderId,
                    CustomerName = d.Order.CustomerName,
                    CustomerPhone = d.Order.CustomerPhone,
                    DeliveryAddress = d.Order.DeliveryAddress,
                    OrderNotes = d.Order.Notes,
                    DeliveryNotes = d.Notes,
                    TotalAmount = d.Order.TotalAmount,
                    Status = d.Status.ToString(),
                    AssignedTime = d.AssignedTime,
                    PickupTime = d.PickupTime,
                    DeliveryTime = d.DeliveryTime
                })
                .ToListAsync();

            return deliveries;
        }

        // GET: api/delivery/active/rider/{id}
        [HttpGet("active/rider/{id}")]
        public async Task<ActionResult<IEnumerable<DeliveryDto>>> GetActiveRiderDeliveries(int id)
        {
            var rider = await _context.Riders.FindAsync(id);
            if (rider == null)
            {
                return NotFound();
            }

            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Where(d => d.RiderId == id && d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Failed)
                .OrderByDescending(d => d.AssignedTime)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    OrderId = d.OrderId,
                    CustomerName = d.Order.CustomerName,
                    CustomerPhone = d.Order.CustomerPhone,
                    DeliveryAddress = d.Order.DeliveryAddress,
                    OrderNotes = d.Order.Notes,
                    DeliveryNotes = d.Notes,
                    TotalAmount = d.Order.TotalAmount,
                    Status = d.Status.ToString(),
                    AssignedTime = d.AssignedTime,
                    PickupTime = d.PickupTime,
                    DeliveryTime = d.DeliveryTime
                })
                .ToListAsync();

            return deliveries;
        }

        // GET: api/delivery/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryDto>> GetDelivery(int id)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            var deliveryDto = new DeliveryDto
            {
                Id = delivery.Id,
                OrderId = delivery.OrderId,
                CustomerName = delivery.Order.CustomerName,
                CustomerPhone = delivery.Order.CustomerPhone,
                DeliveryAddress = delivery.Order.DeliveryAddress,
                OrderNotes = delivery.Order.Notes,
                DeliveryNotes = delivery.Notes,
                TotalAmount = delivery.Order.TotalAmount,
                Status = delivery.Status.ToString(),
                AssignedTime = delivery.AssignedTime,
                PickupTime = delivery.PickupTime,
                DeliveryTime = delivery.DeliveryTime,
                RiderName = delivery.Rider.Name,
                RiderId = delivery.RiderId
            };

            return deliveryDto;
        }

        // PUT: api/delivery/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateDeliveryStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Rider)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            if (!Enum.TryParse<DeliveryStatus>(statusDto.Status, out var newStatus))
            {
                return BadRequest("Invalid status value");
            }

            delivery.Status = newStatus;

            // Update timestamps based on status
            if (newStatus == DeliveryStatus.PickedUp && !delivery.PickupTime.HasValue)
            {
                delivery.PickupTime = DateTime.Now;
            }
            else if (newStatus == DeliveryStatus.Delivered && !delivery.DeliveryTime.HasValue)
            {
                delivery.DeliveryTime = DateTime.Now;
                delivery.Rider.IsAvailable = true;
                delivery.Rider.TotalDeliveries++;
                delivery.Order.Status = OrderStatus.Delivered;
            }
            else if (newStatus == DeliveryStatus.Failed)
            {
                delivery.Rider.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            // Notify connected clients
            await _hubContext.Clients.All.SendAsync("DeliveryStatusUpdated", delivery.Id, newStatus);

            return Ok(new
            {
                message = "Delivery status updated successfully",
                deliveryId = delivery.Id,
                newStatus = delivery.Status.ToString()
            });
        }

        // PUT: api/delivery/rider/{id}/status
        [HttpPut("rider/{id}/status")]
        public async Task<IActionResult> UpdateRiderStatus(int id, [FromBody] UpdateRiderStatusDto statusDto)
        {
            var rider = await _context.Riders.FindAsync(id);
            if (rider == null)
            {
                return NotFound();
            }

            rider.IsAvailable = statusDto.IsAvailable;
            rider.LastActive = DateTime.Now;
            
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Rider status updated successfully",
                riderId = rider.Id,
                isAvailable = rider.IsAvailable
            });
        }
    }

    public class DeliveryDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }
        public string? OrderNotes { get; set; }
        public string? DeliveryNotes { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime AssignedTime { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public string? RiderName { get; set; }
        public int RiderId { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; }
    }

    public class UpdateRiderStatusDto
    {
        public bool IsAvailable { get; set; }
    }
}
