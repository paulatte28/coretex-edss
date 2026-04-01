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

        public string Status { get; set; } = "Completed";

        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
