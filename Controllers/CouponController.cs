using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Controllers
{
    public class CouponController : Controller
    {
        private readonly AppDbContext _context;

        public CouponController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var coupons = _context.Coupons.ToList();
            return View(coupons);
        }

        public IActionResult Validate()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Validate(string code)
        {
            var now = DateTime.Now;
            var coupon = _context.Coupons
                .FirstOrDefault(c => c.Code == code && c.IsActive &&
                                    c.ValidFrom <= now && c.ValidUntil >= now);

            if (coupon != null)
                ViewBag.Message = $"Valid Coupon: {coupon.Code} - {coupon.DiscountValue}% off";
            else
                ViewBag.Message = "Invalid or expired coupon.";

            return View();
        }
        
        // GET: Coupon/ValidateForCheckout?code=COUPON123
        [HttpGet]
        public IActionResult ValidateForCheckout(string code)
        {
            var now = DateTime.Now;
            var coupon = _context.Coupons
                .FirstOrDefault(c => c.Code == code && c.IsActive &&
                                    c.ValidFrom <= now && c.ValidUntil >= now);

            if (coupon != null)
            {
                return Json(new { 
                    valid = true, 
                    coupon = new {
                        code = coupon.Code,
                        discountValue = coupon.DiscountValue
                    }
                });
            }
            else
            {
                return Json(new { 
                    valid = false, 
                    message = "Invalid or expired coupon code." 
                });
            }
        }
        
        // GET: Coupon/ApplyToOrder
        public IActionResult ApplyToOrder(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null)
            {
                return NotFound();
            }
            
            ViewBag.OrderId = orderId;
            ViewBag.OrderTotal = order.TotalAmount;
            return View();
        }
        
        // POST: Coupon/ApplyToOrder
        [HttpPost]
        public async Task<IActionResult> ApplyToOrder(int orderId, string couponCode)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            
            var now = DateTime.Now;
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive &&
                                        c.ValidFrom <= now && c.ValidUntil >= now);
                                        
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired coupon.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }
            
            // Apply the coupon to the order
            order.CouponId = coupon.CouponId;
            
            // Optional: Recalculate the order total with discount
            decimal discountAmount = order.TotalAmount * (coupon.DiscountValue / 100.0m);
            order.TotalAmount -= discountAmount;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Coupon {coupon.Code} applied successfully! You saved {discountAmount:C}";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }
    }
}
