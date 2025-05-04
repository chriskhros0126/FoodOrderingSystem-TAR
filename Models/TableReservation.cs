using System;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class TableReservation
    {
        public int Id { get; set; }

        [Required]
        public int TableNumber { get; set; }

        [Required]
        public int NumberOfGuests { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerPhone { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string CustomerEmail { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
} 