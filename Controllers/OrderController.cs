using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(AppDbContext context, IHubContext<OrderHub> hubContext, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            var dishes = await _context.Dishes.ToListAsync();
            ViewBag.Dishes = dishes;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.OrderItems == null || !model.OrderItems.Any())
            {
                return BadRequest("Please select at least one item");
            }

            var order = new Order
            {
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                DeliveryAddress = model.DeliveryAddress,
                Notes = model.Notes,
                OrderDate = DateTime.Now,
                Status = OrderStatus.New,
                OrderItems = new List<OrderItem>()
            };

            // If user is logged in, associate the order with their account
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    order.UserId = user.Id;
                }
            }

            decimal totalAmount = 0;

            foreach (var item in model.OrderItems)
            {
                var dish = await _context.Dishes.FindAsync(item.DishId);
                if (dish == null)
                {
                    return BadRequest($"Dish with ID {item.DishId} not found");
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest($"Invalid quantity for dish {dish.Name}");
                }

                var orderItem = new OrderItem
                {
                    DishId = item.DishId,
                    Quantity = item.Quantity,
                    UnitPrice = dish.Price
                };

                totalAmount += dish.Price * item.Quantity;
                order.OrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;

            // Apply coupon if provided
            if (!string.IsNullOrEmpty(model.CouponCode))
            {
                var now = DateTime.Now;
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == model.CouponCode && c.IsActive &&
                                            c.ValidFrom <= now && c.ValidUntil >= now);
                                    
                if (coupon != null)
                {
                    // Apply the coupon to the order
                    order.CouponId = coupon.CouponId;
                    
                    // Recalculate the order total with discount
                    decimal discountAmount = order.TotalAmount * (coupon.DiscountValue / 100.0m);
                    order.TotalAmount -= discountAmount;
                }
            }

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Notify all connected clients about the new order
                await _hubContext.Clients.All.SendAsync("NewOrderReceived", order.Id);

                return Json(new { success = true, orderId = order.Id });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to place order: {ex.Message}");
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> GetOrders(OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return PartialView("_OrdersList", orders);
        }

        public async Task<IActionResult> UpdateStatus(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            // Notify all connected clients about the status update
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", id, newStatus);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id, bool partial = false)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .Include(o => o.Coupon)
                .Include(o => o.Feedbacks)
                .Include(o => o.Payments)
                .Include(o => o.Invoices)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (partial)
            {
                return PartialView("_OrderDetailsPartial", order);
            }

            return View(order);
        }

        public IActionResult Pending()
        {
            return View();
        }

        public IActionResult Delivered()
        {
            return View();
        }

        public async Task<IActionResult> GetPendingOrders(OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return PartialView("_OrdersList", orders);
        }

        public async Task<IActionResult> GetDeliveredOrders(DateTime? date = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .Where(o => o.Status == OrderStatus.Delivered);

            if (date.HasValue)
            {
                query = query.Where(o => o.OrderDate.Date == date.Value.Date);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return PartialView("_OrdersList", orders);
        }
    }

    public class OrderCreateViewModel
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }
        public string? Notes { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
        public string? CouponCode { get; set; }
    }

    public class OrderItemViewModel
    {
        public int DishId { get; set; }
        public int Quantity { get; set; }
    }
}