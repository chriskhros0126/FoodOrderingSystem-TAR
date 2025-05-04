using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;

namespace FoodOrderingSystem.Controllers
{
    public class TableReservationController : Controller
    {
        private readonly AppDbContext _context;

        public TableReservationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TableReservation
        public async Task<IActionResult> Index()
        {
            return View(await _context.TableReservations
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync());
        }

        // GET: TableReservation/Calendar
        public async Task<IActionResult> Calendar()
        {
            var reservations = await _context.TableReservations
                .Where(r => r.ReservationDate >= DateTime.Today)
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            return View(reservations);
        }

        // GET: TableReservation/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TableReservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TableNumber,NumberOfGuests,ReservationDate,StartTime,EndTime,CustomerName,CustomerPhone,CustomerEmail,SpecialRequests")] TableReservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Ensure EndTime is at least 1 hour after StartTime
                if (reservation.EndTime <= reservation.StartTime)
                {
                    reservation.EndTime = reservation.StartTime.Add(TimeSpan.FromHours(2));
                }

                // Check if table is available
                var isAvailable = await IsTableAvailable(reservation);
                if (!isAvailable)
                {
                    ModelState.AddModelError("", "This table is not available for the selected time slot.");
                    return View(reservation);
                }

                // Set the created time and default status
                reservation.CreatedAt = DateTime.UtcNow;
                reservation.Status = ReservationStatus.Confirmed;

                _context.Add(reservation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservation created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(reservation);
        }

        // GET: TableReservation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.TableReservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return View(reservation);
        }

        // POST: TableReservation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TableNumber,NumberOfGuests,ReservationDate,StartTime,EndTime,CustomerName,CustomerPhone,CustomerEmail,SpecialRequests,Status")] TableReservation reservation)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
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
            return View(reservation);
        }

        // POST: TableReservation/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.TableReservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsTableAvailable(TableReservation newReservation)
        {
            var conflictingReservations = await _context.TableReservations
                .Where(r => r.TableNumber == newReservation.TableNumber
                    && r.ReservationDate == newReservation.ReservationDate
                    && r.Status != ReservationStatus.Cancelled
                    && ((newReservation.StartTime >= r.StartTime && newReservation.StartTime < r.EndTime)
                        || (newReservation.EndTime > r.StartTime && newReservation.EndTime <= r.EndTime)
                        || (newReservation.StartTime <= r.StartTime && newReservation.EndTime >= r.EndTime)))
                .ToListAsync();

            return !conflictingReservations.Any();
        }

        private bool ReservationExists(int id)
        {
            return _context.TableReservations.Any(e => e.Id == id);
        }
    }
} 