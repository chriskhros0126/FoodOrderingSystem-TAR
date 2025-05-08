using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Hubs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

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

        [Authorize]
        public async Task<IActionResult> Create()
        {
            var dishes = await _context.Dishes.ToListAsync();
            ViewBag.Dishes = dishes;
            return View();
        }

        [HttpPost]
        [Authorize]
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

            // Validate inventory before processing order
            var (isValid, errorMessage) = await ValidateInventoryForOrder(model);
            if (!isValid)
            {
                return BadRequest(errorMessage);
            }

            // Create a temporary order ID
            var tempOrderId = Guid.NewGuid().ToString();

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
            var orderItemDetails = new List<OrderItemDetail>();

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

                // Store dish details for confirmation page
                orderItemDetails.Add(new OrderItemDetail
                {
                    OrderItemId = 0, // This will be set later
                    DishId = dish.Id,
                    DishName = dish.Name,
                    UnitPrice = dish.Price,
                    Quantity = item.Quantity
                });

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
                // Store the order temporarily in TempData
                TempData["PendingOrder"] = JsonSerializer.Serialize(order);
                // Store order item details separately
                TempData["OrderItemDetails"] = JsonSerializer.Serialize(orderItemDetails);
                // Store the temporary order ID
                TempData["TempOrderId"] = tempOrderId;
                
                return Json(new { success = true, tempOrderId = tempOrderId });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to process order: {ex.Message}");
            }
        }

        [Authorize]
        public IActionResult Confirmation()
        {
            // Get the pending order from TempData
            if (TempData["PendingOrder"] == null || TempData["OrderItemDetails"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var orderJson = TempData["PendingOrder"].ToString();
            var orderItemDetailsJson = TempData["OrderItemDetails"].ToString();
            
            // Keep the data in TempData for the payment process
            TempData.Keep("PendingOrder");
            TempData.Keep("OrderItemDetails");
            
            var order = JsonSerializer.Deserialize<Order>(orderJson);
            var orderItemDetails = JsonSerializer.Deserialize<List<OrderItemDetail>>(orderItemDetailsJson);

            // Create a view model that combines both
            var viewModel = new OrderConfirmationViewModel
            {
                Order = order,
                OrderItemDetails = orderItemDetails
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize]
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
        [Authorize]
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

        [Authorize]
        public async Task<IActionResult> Details(int id, bool partial = false)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
                .Include(o => o.Coupon)
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

        [Authorize(Roles = "Admin")]
        public IActionResult Pending()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delivered()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
        
        private async Task<(bool IsValid, string ErrorMessage)> ValidateInventoryForOrder(OrderCreateViewModel model)
        {
            foreach (var item in model.OrderItems)
            {
                var dish = await _context.Dishes
                    .Include(d => d.DishIngredients)
                    .ThenInclude(di => di.InventoryItem)
                    .FirstOrDefaultAsync(d => d.Id == item.DishId);
                    
                if (dish == null)
                {
                    return (false, $"Dish with ID {item.DishId} not found");
                }
                
                // Check if there are enough ingredients in stock
                foreach (var ingredient in dish.DishIngredients)
                {
                    var requiredQuantity = ingredient.QuantityRequired * item.Quantity;
                    if (ingredient.InventoryItem.CurrentStock < requiredQuantity)
                    {
                        return (false, $"Not enough {ingredient.InventoryItem.Name} in stock to fulfill this order");
                    }
                }
            }
            
            return (true, string.Empty);
        }
        
        private async Task DeductIngredientsFromInventory(int dishId, int quantity)
        {
            var dish = await _context.Dishes
                .Include(d => d.DishIngredients)
                .ThenInclude(di => di.InventoryItem)
                .FirstOrDefaultAsync(d => d.Id == dishId);
                
            if (dish == null) return;
            
            foreach (var ingredient in dish.DishIngredients)
            {
                var inventoryItem = ingredient.InventoryItem;
                var deductAmount = ingredient.QuantityRequired * quantity;
                
                inventoryItem.CurrentStock -= deductAmount;
                inventoryItem.LastUpdated = DateTime.UtcNow;
                
                _context.Update(inventoryItem);
            }
            
            await _context.SaveChangesAsync();
        }
        
        // Method to update inventory after an order is confirmed
        private async Task UpdateInventoryForOrder(Order order)
        {
            foreach (var orderItem in order.OrderItems)
            {
                // Get the dish with its ingredients
                var dish = await _context.Dishes
                    .Include(d => d.DishIngredients)
                    .ThenInclude(di => di.InventoryItem)
                    .FirstOrDefaultAsync(d => d.Id == orderItem.DishId);
                    
                if (dish != null)
                {
                    foreach (var dishIngredient in dish.DishIngredients)
                    {
                        // Calculate how much of this ingredient is used
                        decimal amountUsed = dishIngredient.QuantityRequired * orderItem.Quantity;
                        
                        // Update inventory
                        var inventoryItem = await _context.InventoryItems.FindAsync(dishIngredient.InventoryItemId);
                        if (inventoryItem != null)
                        {
                            inventoryItem.CurrentStock -= amountUsed;
                            inventoryItem.LastUpdated = DateTime.UtcNow;
                            
                            // Ensure stock doesn't go below zero
                            if (inventoryItem.CurrentStock < 0)
                            {
                                inventoryItem.CurrentStock = 0;
                            }
                            
                            _context.Update(inventoryItem);
                        }
                    }
                }
            }
            
            await _context.SaveChangesAsync();
        }
        
        // Method to finalize the order and update inventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeOrder()
        {
            // Get the pending order from TempData
            if (TempData["PendingOrder"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var orderJson = TempData["PendingOrder"].ToString();
            var order = JsonSerializer.Deserialize<Order>(orderJson);
            
            // Validate inventory before finalizing
            var model = new OrderCreateViewModel
            {
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel 
                { 
                    DishId = oi.DishId, 
                    Quantity = oi.Quantity 
                }).ToList()
            };
            
            var (isValid, errorMessage) = await ValidateInventoryForOrder(model);
            if (!isValid)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Confirmation");
            }
            
            // Save the order to the database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            
            // Update inventory
            await UpdateInventoryForOrder(order);
            
            // Clear TempData
            TempData.Remove("PendingOrder");
            TempData.Remove("OrderItemDetails");
            
            // Notify connected clients about the new order
            await _hubContext.Clients.All.SendAsync("NewOrderReceived", order.Id);
            
            return RedirectToAction("OrderSuccess", new { id = order.Id });
        }
        
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }

    // Move these classes outside of the controller class but still within the namespace
    public class OrderCreateViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
        public string? CouponCode { get; set; }
    }

    public class OrderItemViewModel
    {
        public int DishId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderItemDetail
    {
        public int OrderItemId { get; set; }
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderConfirmationViewModel
    {
        public Order Order { get; set; } = null!;
        public List<OrderItemDetail> OrderItemDetails { get; set; } = new List<OrderItemDetail>();
    }
}