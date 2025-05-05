// ADMIN ONLY

using Microsoft.AspNetCore.Mvc;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string sortOrder, string category, bool? specialOnly)
        {
            var dishes = _context.Dishes.AsQueryable();

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                dishes = dishes.Where(d => d.Category == category);
            }

            // Apply special filter
            if (specialOnly.HasValue && specialOnly.Value)
            {
                dishes = dishes.Where(d => d.IsSpecialToday);
            }

            // Apply sorting
            dishes = sortOrder?.ToLower() switch
            {
                "price_asc" => dishes.OrderBy(d => d.Price),
                "price_desc" => dishes.OrderByDescending(d => d.Price),
                _ => dishes.OrderBy(d => d.Name) // Default sorting
            };

            // Get unique categories for the filter dropdown
            ViewBag.Categories = _context.Dishes.Select(d => d.Category).Distinct().ToList();
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentCategory = category;
            ViewBag.SpecialOnly = specialOnly;

            return View(dishes.ToList());
        }

        public IActionResult Create() // GET AND POST
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Dish dish)
        {
            if (ModelState.IsValid)
            {
                // Ensure the ImageUrl starts with /images/
                if (!string.IsNullOrEmpty(dish.ImageUrl) && !dish.ImageUrl.StartsWith("/images/"))
                {
                    dish.ImageUrl = "/images/dishes/" + Path.GetFileName(dish.ImageUrl);
                }

                _context.Dishes.Add(dish);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(dish);
        }

        [HttpPost]
        public IActionResult BulkImport(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a CSV file.");
                return View("Create");
            }

            try
            {
                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<Dish>().ToList();
                    
                    foreach (var dish in records)
                    {
                        if (string.IsNullOrWhiteSpace(dish.Name) || dish.Price <= 0)
                        {
                            continue; // Skip invalid records
                        }

                        // Fix image URL format
                        if (!string.IsNullOrEmpty(dish.ImageUrl))
                        {
                            dish.ImageUrl = "/images/dishes/" + Path.GetFileName(dish.ImageUrl);
                        }

                        _context.Dishes.Add(dish);
                    }

                    _context.SaveChanges();
                }

                TempData["Message"] = "Dishes imported successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error importing dishes: {ex.Message}");
                return View("Create");
            }
        }

        public IActionResult Edit(int id)
        {
            var dish = _context.Dishes.Find(id);
            return View(dish);
        }

        [HttpPost]
        public IActionResult Edit(Dish dish)
        {
            if (ModelState.IsValid)
            {
                // Fix image URL format
                if (!string.IsNullOrEmpty(dish.ImageUrl) && !dish.ImageUrl.StartsWith("/images/"))
                {
                    dish.ImageUrl = "/images/dishes/" + Path.GetFileName(dish.ImageUrl);
                }

                _context.Dishes.Update(dish);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(dish);
        }

        public IActionResult Delete(int id)
        {
            var dish = _context.Dishes.Find(id);
            return View(dish);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var dish = _context.Dishes.Find(id);
            if (dish != null)
            {
                _context.Dishes.Remove(dish);
                _context.SaveChanges();
            }            
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var dish = _context.Dishes.Find(id);
            return View(dish);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability([FromBody] ToggleAvailabilityRequest request)
        {
            try
            {
                if (request == null || request.id == 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                var dish = await _context.Dishes.FindAsync(request.id);
                if (dish == null)
                {
                    return NotFound(new { success = false, message = "Dish not found" });
                }

                dish.IsAvailable = !dish.IsAvailable;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isAvailable = dish.IsAvailable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error updating availability: {ex.Message}" });
            }
        }

        public class ToggleAvailabilityRequest
        {
            public int id { get; set; }
        }

        public IActionResult DownloadSampleCsv()
        {
            var sampleDishes = new List<Dish>
            {
                new Dish
                {
                    Name = "Spaghetti Carbonara",
                    Price = 15.99m,
                    Category = "Mains",
                    Description = "Classic Italian pasta dish with eggs, cheese, pancetta, and black pepper",
                    ImageUrl = "/images/dishes/SpaghettiCarbonara.jpg",
                    IsAvailable = true,
                    IsSpecialToday = false
                },
                new Dish
                {
                    Name = "Chocolate Lava Cake",
                    Price = 8.99m,
                    Category = "Desserts",
                    Description = "Warm chocolate cake with a molten center, served with vanilla ice cream",
                    ImageUrl = "/images/dishes/Chocolate-Lava-Cake-Recipe.jpg",
                    IsAvailable = true,
                    IsSpecialToday = true
                },
                new Dish
                {
                    Name = "Gourmet Beef Burger",
                    Price = 16.99m,
                    Category = "Mains",
                    Description = "Premium beef patty with caramelized onions, fresh lettuce, and special sauce on a brioche bun",
                    ImageUrl = "/images/dishes/burger.jpg",
                    IsAvailable = true,
                    IsSpecialToday = true
                },
                new Dish
                {
                    Name = "Creamy Mushroom Soup",
                    Price = 7.99m,
                    Category = "Appetizers",
                    Description = "Rich and creamy soup with fresh mushrooms, herbs, and a touch of truffle oil",
                    ImageUrl = "/images/dishes/Mushroom-soup.jpg",
                    IsAvailable = true,
                    IsSpecialToday = false
                },
                new Dish
                {
                    Name = "Pepperoni Pizza",
                    Price = 18.99m,
                    Category = "Mains",
                    Description = "Classic pizza with tomato sauce, mozzarella cheese, and spicy pepperoni",
                    ImageUrl = "/images/dishes/pizza.jpg",
                    IsAvailable = true,
                    IsSpecialToday = false
                },
                new Dish
                {
                    Name = "Caesar Salad",
                    Price = 12.99m,
                    Category = "Appetizers",
                    Description = "Crisp romaine lettuce, parmesan cheese, croutons, and classic Caesar dressing",
                    ImageUrl = "/images/dishes/salad.jpg",
                    IsAvailable = true,
                    IsSpecialToday = false
                }
            };

            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(sampleDishes);
                var content = writer.ToString();
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/csv", "sample-dishes.csv");
            }
        }
    }
}
