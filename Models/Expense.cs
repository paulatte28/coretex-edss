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

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}
