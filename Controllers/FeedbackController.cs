using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var feedbackList = _context.Feedbacks
                .Include(f => f.Order)
                .ToList();
            return View(feedbackList);
        }

        public IActionResult Submit(int? orderId)
        {
            if (orderId == null)
            {
                return BadRequest("Order ID is required");
            }

            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound($"Order with ID {orderId} not found");
            }

            // Check if feedback already exists for this order
            var existingFeedback = _context.Feedbacks.FirstOrDefault(f => f.OrderId == orderId);
            if (existingFeedback != null)
            {
                TempData["Message"] = "You have already submitted feedback for this order";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            ViewBag.OrderId = orderId;
            ViewBag.OrderDetails = order;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                feedback.DateSubmitted = DateTime.Now;
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ThankYou), new { orderId = feedback.OrderId });
            }
            
            // If model state is invalid, repopulate the ViewBag data
            var order = await _context.Orders.FindAsync(feedback.OrderId);
            ViewBag.OrderId = feedback.OrderId;
            ViewBag.OrderDetails = order;
            return View(feedback);
        }

        public IActionResult ThankYou(int? orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}
