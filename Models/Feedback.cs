using System;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackID { get; set; }
        
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;
        
        [Required]
        public string Comment { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }
} 