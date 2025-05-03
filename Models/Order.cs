using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.New;

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string DeliveryAddress { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        public string? Notes { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int DishId { get; set; }
        public Dish Dish { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public enum OrderStatus
    {
        New,
        Confirmed,
        Preparing,
        OutForDelivery,
        Delivered,
        InTransit,
        Cancelled
    }
}
