using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FoodOrderingSystem.Hubs;
using System.Text.Json;

namespace FoodOrderingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public PaymentController(AppDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: /Payment/
        public IActionResult Index()
        {
            var payments = _context.Payments
                .Include(p => p.Order)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
            return View(payments);
        }

        // GET: /Payment/Create
        public IActionResult Create()
        {
            if (TempData["PendingOrder"] == null || TempData["OrderItemDetails"] == null || TempData["TempOrderId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var orderJson = TempData["PendingOrder"].ToString();
            var orderItemDetailsJson = TempData["OrderItemDetails"].ToString();
            var tempOrderId = TempData["TempOrderId"].ToString();
            
            // Keep the data in TempData for form submission
            TempData.Keep("PendingOrder");
            TempData.Keep("OrderItemDetails");
            TempData.Keep("TempOrderId");
            
            var order = JsonSerializer.Deserialize<Order>(orderJson);

            var payment = new Payment
            {
                PaymentMethod = "", // Will be set by form
                PaymentDate = DateTime.Now,
                PaymentStatus = "Pending",
                Amount = order.TotalAmount
            };

            ViewBag.PendingOrderTotal = order.TotalAmount;
            ViewBag.TempOrderId = tempOrderId;

            return View(payment);
        }


        // POST: /Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string PaymentMethod, decimal Amount, string? CouponCode = null, string tempOrderId = null, string? AdminNotes = null)
        {
            // Ensure CouponCode is empty string if it's null or whitespace
            CouponCode = string.IsNullOrWhiteSpace(CouponCode) ? "" : CouponCode;

            if (TempData["PendingOrder"] == null || TempData["OrderItemDetails"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Declare all variables once at the beginning
            string pendingOrderJson = TempData["PendingOrder"].ToString();
            string orderItemDetailsJson = TempData["OrderItemDetails"].ToString();
            Order pendingOrderData = null;
            List<FoodOrderingSystem.Controllers.OrderItemDetail> orderItemDetails = null;

            // Keep TempData for potential errors
            TempData.Keep("PendingOrder");
            TempData.Keep("OrderItemDetails");
            TempData.Keep("TempOrderId");

            // Check required fields
            if (string.IsNullOrEmpty(PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "Payment method is required");
                
                pendingOrderData = JsonSerializer.Deserialize<Order>(pendingOrderJson);
                ViewBag.PendingOrderTotal = pendingOrderData.TotalAmount;
                ViewBag.TempOrderId = tempOrderId;
                
                return View(new Payment { 
                    Amount = Amount,
                    PaymentMethod = PaymentMethod,
                    CouponCode = CouponCode
                });
            }

            // Set ViewBag for amount in case we need to return to the view
            pendingOrderData = JsonSerializer.Deserialize<Order>(pendingOrderJson);
            orderItemDetails = JsonSerializer.Deserialize<List<FoodOrderingSystem.Controllers.OrderItemDetail>>(orderItemDetailsJson);
            ViewBag.PendingOrderTotal = pendingOrderData.TotalAmount;
            ViewBag.TempOrderId = tempOrderId;

            if (!string.IsNullOrEmpty(CouponCode))
            {
                var now = DateTime.Now;
                var coupon = _context.Coupons
                    .FirstOrDefault(c => c.Code == CouponCode && c.IsActive &&
                                         c.ValidFrom <= now && c.ValidUntil >= now);
                if (coupon != null)
                {
                    // Apply discount
                    Amount -= Amount * (coupon.DiscountValue / 100.0m);
                    pendingOrderData.CouponId = coupon.CouponId;
                    pendingOrderData.TotalAmount = Amount;
                }
                else if (CouponCode.Trim() != "")
                {
                    ModelState.AddModelError("CouponCode", "Invalid or expired coupon.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Create a completely new order entity
                    var newOrder = new Order
                    {
                        CustomerName = pendingOrderData.CustomerName,
                        CustomerPhone = pendingOrderData.CustomerPhone,
                        DeliveryAddress = pendingOrderData.DeliveryAddress,
                        Notes = pendingOrderData.Notes,
                        OrderDate = DateTime.Now,
                        Status = OrderStatus.Confirmed,
                        UserId = pendingOrderData.UserId,
                        TotalAmount = pendingOrderData.TotalAmount,
                        CouponId = pendingOrderData.CouponId,
                        OrderItems = new List<OrderItem>()
                    };

                    // Add to database first to get an ID
                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();

                    // Create order items
                    foreach (var itemDetail in orderItemDetails)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = newOrder.Id,
                            DishId = itemDetail.DishId,
                            Quantity = itemDetail.Quantity,
                            UnitPrice = itemDetail.UnitPrice
                        };
                        
                        _context.OrderItems.Add(orderItem);
                    }
                    
                    await _context.SaveChangesAsync();

                    // Create and save the payment
                    var newPayment = new Payment
                    {
                        OrderId = newOrder.Id,
                        PaymentMethod = PaymentMethod,
                        Amount = pendingOrderData.TotalAmount,
                        PaymentDate = DateTime.Now,
                        PaymentStatus = "Completed",
                        CouponCode = string.IsNullOrWhiteSpace(CouponCode) ? "" : CouponCode
                    };
                    
                    // Store admin notes in the Notes property if it exists in the Payment model
                    // If it doesn't exist, you'll need to add it to the Payment model
                    if (!string.IsNullOrWhiteSpace(AdminNotes) && PaymentMethod == "Manual Entry")
                    {
                        // We can't directly add Notes to the Payment model without modifying it,
                        // so let's store it in the OrderNotes for now
                        newOrder.Notes = (string.IsNullOrWhiteSpace(newOrder.Notes) ? "" : newOrder.Notes + "\n\n") + 
                            $"ADMIN PAYMENT NOTE [{DateTime.Now:g}]: {AdminNotes}";
                        await _context.SaveChangesAsync();
                    }
                    
                    _context.Payments.Add(newPayment);
                    await _context.SaveChangesAsync();

                    // Clear the temporary order data
                    TempData.Remove("PendingOrder");
                    TempData.Remove("OrderItemDetails");
                    TempData.Remove("TempOrderId");

                    // Notify all connected clients about the new order
                    await _hubContext.Clients.All.SendAsync("NewOrderReceived", newOrder.Id);

                    // Store payment ID in TempData as a backup method to retrieve the payment
                    TempData["LastPaymentId"] = newPayment.PaymentId;

                    // Redirect to payment received page with explicit controller and action
                    return RedirectToAction("PaymentReceived", "Payment", new { id = newPayment.PaymentId });
                }
                catch (Exception ex)
                {
                    // Log the exception and add it to the model state
                    ModelState.AddModelError("", $"Error processing payment: {ex.Message}");
                    
                    var payment = new Payment {
                        Amount = Amount,
                        PaymentMethod = PaymentMethod,
                        CouponCode = CouponCode
                    };
                    
                    return View(payment);
                }
            }

            var paymentModel = new Payment {
                Amount = Amount,
                PaymentMethod = PaymentMethod,
                CouponCode = CouponCode
            };
            
            return View(paymentModel);
        }

        // GET: Payment/PaymentReceived/5
        [HttpGet]
        [Route("Payment/PaymentReceived/{id?}")]
        public async Task<IActionResult> PaymentReceived(int id = 0)
        {
            Payment payment = null;
            
            // Try to get payment by ID first
            if (id > 0)
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == id);
            }
            
            // If payment not found but we have the ID in TempData, try to get it from there
            if (payment == null && TempData["LastPaymentId"] != null)
            {
                var paymentId = (int)TempData["LastPaymentId"];
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                
                // Keep the payment ID in TempData just in case
                TempData.Keep("LastPaymentId");
            }
                
            if (payment == null)
            {
                // If no payment found, redirect to home
                return RedirectToAction("Index", "Home");
            }
            
            return View(payment);
        }

        // Optional: Details view for a payment
        public IActionResult Details(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Order)
                .FirstOrDefault(p => p.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
        }
    }
}
