using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace FoodOrderingSystem.Models;

public class Payment
{
    public int PaymentId { get; set; }
    [Required]
    public int OrderId { get; set; }
    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; }
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    [Required]
    public string PaymentStatus { get; set; } = "Pending";

    // Navigation property to Order - not required for form submission
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    // Optional coupon code
    [AllowNull]
    [Column(TypeName = "nvarchar(100)")]
    public string CouponCode { get; set; } = string.Empty;
}
