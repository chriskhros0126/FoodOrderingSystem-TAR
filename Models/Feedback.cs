namespace FoodOrderingSystem.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime DateSubmitted { get; set; }

        // Navigation property to Order
        public Order Order { get; set; }

        public string Message { get; set; }
    }
}