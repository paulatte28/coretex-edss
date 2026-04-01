using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 10;

        // Multi-tenancy
        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
