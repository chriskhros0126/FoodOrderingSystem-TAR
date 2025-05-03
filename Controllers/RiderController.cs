using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;

namespace FoodOrderingSystem.Controllers
{
    public class RiderController : Controller
    {
        private readonly AppDbContext _context;

        public RiderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Rider
        public async Task<IActionResult> Index()
        {
            return View(await _context.Riders.ToListAsync());
        }

        // GET: Rider/Details/5
        public async Task<IActionResult> Details(int? id)
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

            return View(rider);
        }

        // GET: Rider/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rider/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PhoneNumber,Email,VehicleType,LicenseNumber")] Rider rider)
        {
            if (ModelState.IsValid)
            {
                rider.LastActive = DateTime.UtcNow;
                _context.Add(rider);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(rider);
        }

        // GET: Rider/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rider = await _context.Riders.FindAsync(id);
            if (rider == null)
            {
                return NotFound();
            }
            return View(rider);
        }

        // POST: Rider/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,PhoneNumber,Email,VehicleType,LicenseNumber,IsAvailable")] Rider rider)
        {
            if (id != rider.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rider);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RiderExists(rider.Id))
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
            return View(rider);
        }

        // GET: Rider/Performance/5
        public async Task<IActionResult> Performance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rider = await _context.Riders
                .Include(r => r.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rider == null)
            {
                return NotFound();
            }

            return View(rider);
        }

        private bool RiderExists(int id)
        {
            return _context.Riders.Any(e => e.Id == id);
        }
    }
} 