using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Sale
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string OrderId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public string ProductName { get; set; } = "General Sale";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }

        public string Status { get; set; } = "Completed";

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}
