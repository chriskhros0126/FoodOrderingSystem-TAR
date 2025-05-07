using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace FoodOrderingSystem.Controllers
{
    public class CouponController : Controller
    {
        private readonly AppDbContext _context;

        public CouponController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Coupon
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.ValidUntil)
                .ToListAsync();
            return View(coupons);
        }

        // GET: Coupon/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                // Check if coupon code already exists
                if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
                {
                    ModelState.AddModelError("Code", "This coupon code already exists.");
                    return View(coupon);
                }

                // Validate dates
                if (coupon.ValidUntil < coupon.ValidFrom)
                {
                    ModelState.AddModelError("ValidUntil", "The expiration date must be after the start date.");
                    return View(coupon);
                }

                _context.Add(coupon);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // GET: Coupon/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        // POST: Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.CouponId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if coupon code already exists (excluding this coupon)
                if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.CouponId != id))
                {
                    ModelState.AddModelError("Code", "This coupon code already exists.");
                    return View(coupon);
                }

                // Validate dates
                if (coupon.ValidUntil < coupon.ValidFrom)
                {
                    ModelState.AddModelError("ValidUntil", "The expiration date must be after the start date.");
                    return View(coupon);
                }

                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' has been updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.CouponId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // GET: Coupon/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CouponId == id);
                
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // POST: Coupon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CouponId == id);
                
            if (coupon == null)
            {
                return NotFound();
            }

            // Check if coupon is in use
            if (coupon.Orders.Any())
            {
                TempData["ErrorMessage"] = $"Coupon '{coupon.Code}' cannot be deleted as it is used in {coupon.Orders.Count} orders. Consider deactivating it instead.";
                return RedirectToAction(nameof(Index));
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' has been deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Coupon/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CouponId == id);
                
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // Customer-facing actions

        // GET: Coupon/Validate
        public IActionResult Validate()
        {
            return View();
        }

        // POST: Coupon/Validate
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
        public async Task<IActionResult> ApplyToOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            
            // Check if order already has a coupon
            if (order.CouponId.HasValue)
            {
                var existingCoupon = await _context.Coupons.FindAsync(order.CouponId);
                if (existingCoupon != null)
                {
                    ViewBag.ExistingCoupon = existingCoupon;
                }
            }
            
            ViewBag.OrderId = orderId;
            ViewBag.OrderTotal = order.TotalAmount;
            return View();
        }
        
        // POST: Coupon/ApplyToOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyToOrder(int orderId, string couponCode)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            
            // If coupon code is empty, return error
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["ErrorMessage"] = "Please enter a coupon code.";
                return RedirectToAction(nameof(ApplyToOrder), new { orderId });
            }
            
            var now = DateTime.Now;
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive &&
                                        c.ValidFrom <= now && c.ValidUntil >= now);
                                        
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired coupon.";
                return RedirectToAction(nameof(ApplyToOrder), new { orderId });
            }
            
            // If order already has this coupon, return error
            if (order.CouponId == coupon.CouponId)
            {
                TempData["ErrorMessage"] = "This coupon is already applied to the order.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }
            
            // If order already has another coupon, remove it first
            if (order.CouponId.HasValue && order.CouponId != coupon.CouponId)
            {
                // Reset the order total to original value (without discount)
                var existingCoupon = await _context.Coupons.FindAsync(order.CouponId);
                if (existingCoupon != null)
                {
                    // Reverse the previous discount
                    decimal originalAmount = order.TotalAmount / (1 - (existingCoupon.DiscountValue / 100.0m));
                    order.TotalAmount = originalAmount;
                }
            }
            
            // Apply the coupon to the order
            order.CouponId = coupon.CouponId;
            
            // Calculate the order total with discount
            decimal discountAmount = order.TotalAmount * (coupon.DiscountValue / 100.0m);
            order.TotalAmount -= discountAmount;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Coupon {coupon.Code} applied successfully! You saved {discountAmount:C}";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }
        
        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }
    }
}
