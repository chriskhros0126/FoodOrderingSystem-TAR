using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FoodOrderingSystem.Models
{

    public class Coupon
    {
        public int CouponId { get; set; }
        public string Code { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; }
        
        // Collection of orders that used this coupon
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

}