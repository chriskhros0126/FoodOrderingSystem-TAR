using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Rider
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string VehicleType { get; set; }

        [Required]
        [StringLength(20)]
        public string LicenseNumber { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime LastActive { get; set; }

        public int TotalDeliveries { get; set; }

        public double Rating { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
