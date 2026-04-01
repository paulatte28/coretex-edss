using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Expense
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
