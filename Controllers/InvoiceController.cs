using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;

        public InvoiceController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var invoices = _context.Invoices
                .Include(i => i.Order)
                .OrderByDescending(i => i.GeneratedDate)
                .ToList();
            return View(invoices);
        }

        public IActionResult Details(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Order)
                .FirstOrDefault(i => i.InvoiceId == id);
            if (invoice == null) return NotFound();
            return View(invoice);
        }

        public IActionResult Generate(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
                return NotFound();

            var invoice = new Invoice
            {
                OrderId = orderId,
                GeneratedDate = DateTime.Now,
                PdfPath = $"/invoices/invoice_{orderId}.pdf"
                // Generate PDF logic would go here
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = invoice.InvoiceId });
        }

        public IActionResult Download(int id)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == id);
            if (invoice == null || string.IsNullOrEmpty(invoice.PdfPath))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", invoice.PdfPath);
            if (!System.IO.File.Exists(filePath))
                return NotFound("PDF file not found.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", $"Invoice_{invoice.InvoiceId}.pdf");
        }
    }
}
