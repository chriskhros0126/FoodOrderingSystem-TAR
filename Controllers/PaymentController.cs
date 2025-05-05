using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
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
        public IActionResult Create(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentDate = DateTime.Now,
                PaymentStatus = "Pending",
                Amount = order.TotalAmount
            };

            return View(payment);
        }


        // POST: /Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment, string couponCode)
        {
            if (!string.IsNullOrEmpty(couponCode))
            {
                var now = DateTime.Now;
                var coupon = _context.Coupons
                    .FirstOrDefault(c => c.Code == couponCode && c.IsActive &&
                                         c.ValidFrom <= now && c.ValidUntil >= now);
                if (coupon != null)
                {
                    // Apply discount
                    payment.Amount -= payment.Amount * (coupon.DiscountValue / 100.0m);
                }
                else
                {
                    ModelState.AddModelError("couponCode", "Invalid or expired coupon.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
