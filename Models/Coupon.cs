using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FoodOrderingSystem.Models
{
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        
        [Required(ErrorMessage = "Coupon code is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Coupon code must be between 3 and 20 characters")]
        [Display(Name = "Coupon Code")]
        public string Code { get; set; }
        
        [Required(ErrorMessage = "Discount value is required")]
        [Range(1, 100, ErrorMessage = "Discount must be between 1% and 100%")]
        [Display(Name = "Discount (%)")]
        public decimal DiscountValue { get; set; }
        
        [Required(ErrorMessage = "Valid from date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Valid From")]
        public DateTime ValidFrom { get; set; } = DateTime.Today;
        
        [Required(ErrorMessage = "Valid until date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Valid Until")]
        public DateTime ValidUntil { get; set; } = DateTime.Today.AddMonths(1);
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        // Collection of orders that used this coupon
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}