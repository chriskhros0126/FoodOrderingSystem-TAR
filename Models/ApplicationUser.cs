using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Models
{
    public class ApplicationUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public virtual IdentityUser IdentityUser { get; set; }
    }
} 