using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Controllers
{
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            return View(await _context.InventoryItems.ToListAsync());
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // GET: Inventory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,CurrentStock,Unit,Threshold,CostPerUnit")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                inventoryItem.LastUpdated = DateTime.UtcNow;
                _context.Add(inventoryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(inventoryItem);
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }
            return View(inventoryItem);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,CurrentStock,Unit,Threshold,CostPerUnit")] InventoryItem inventoryItem)
        {
            if (id != inventoryItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    inventoryItem.LastUpdated = DateTime.UtcNow;
                    _context.Update(inventoryItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryItemExists(inventoryItem.Id))
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
            return View(inventoryItem);
        }

        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            var lowStockItems = await _context.InventoryItems
                .Where(i => i.CurrentStock <= i.Threshold)
                .OrderBy(i => i.CurrentStock)
                .ToListAsync();

            return View(lowStockItems);
        }

        // POST: Inventory/Restock/5
        [HttpPost]
        public async Task<IActionResult> Restock(int id, decimal quantity)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item != null)
            {
                item.CurrentStock += quantity;
                item.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{item.Name} restocked successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventory/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = new
            {
                TotalItems = await _context.InventoryItems.CountAsync(),
                LowStockItems = await _context.InventoryItems
                                    .Where(i => i.CurrentStock <= i.Threshold)
                                    .CountAsync(),
                OutOfStockItems = await _context.InventoryItems
                                        .Where(i => i.CurrentStock <= 0)
                                        .CountAsync()
            };

            return View(dashboardData);
        }

        private bool InventoryItemExists(int id)
        {
            return _context.InventoryItems.Any(e => e.Id == id);
        }
    }
}