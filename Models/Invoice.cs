using System;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{

    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string PdfPath { get; set; }

        // Navigation property to Order
        public Order Order { get; set; }
    }

}
