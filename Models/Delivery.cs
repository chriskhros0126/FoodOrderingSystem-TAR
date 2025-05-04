using System;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Delivery
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        [Required]
        public int RiderId { get; set; }
        public virtual Rider Rider { get; set; }

        public DateTime AssignedTime { get; set; } = DateTime.Now;

        public DateTime? PickupTime { get; set; }

        public DateTime? DeliveryTime { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;

        public string? Notes { get; set; }
    }

    public enum DeliveryStatus
    {
        Assigned,
        PickedUp,
        InTransit,
        Delivered,
        Failed
    }
}
