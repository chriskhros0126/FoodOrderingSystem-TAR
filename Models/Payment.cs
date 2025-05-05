namespace FoodOrderingSystem.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentStatus { get; set; }

    // Navigation property to Order
    public Order Order { get; set; }

    public string CouponCode { get; set; }
}
