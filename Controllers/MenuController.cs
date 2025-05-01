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

namespace FoodOrderingSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var dishes = _context.Dishes.ToList();
            return View(dishes);        
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
                    ImageUrl = "https://example.com/carbonara.jpg",
                    IsAvailable = true,
                    IsSpecialToday = false
                },
                new Dish
                {
                    Name = "Chocolate Lava Cake",
                    Price = 8.99m,
                    Category = "Desserts",
                    Description = "Warm chocolate cake with a molten center, served with vanilla ice cream",
                    ImageUrl = "https://example.com/lava-cake.jpg",
                    IsAvailable = true,
                    IsSpecialToday = true
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
