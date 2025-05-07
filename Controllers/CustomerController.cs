using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;

        public CustomerController(
            UserManager<IdentityUser> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Customer/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var appUser = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (appUser == null)
            {
                return NotFound();
            }

            var profileViewModel = new ProfileViewModel
            {
                Id = appUser.Id,
                Name = appUser.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = appUser.Address
            };

            return View(profileViewModel);
        }

        // POST: Customer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var appUser = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (appUser == null)
            {
                return NotFound();
            }

            // Update identity user details
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            
            // Update application user details
            appUser.Name = model.Name;
            appUser.Address = model.Address;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // GET: Customer/OrderHistory
        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Include(o => o.Coupon)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Customer/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Include(o => o.Coupon)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
} 