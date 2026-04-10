using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class KpiThreshold
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Range(0, 100)]
        public decimal MinProfitMargin { get; set; }

        [Range(0, 100)]
        public decimal MaxExpenseRatio { get; set; }

        public decimal MinMonthlyProfit { get; set; }

        [Required]
        [StringLength(20)]
        public string RiskAlertLevel { get; set; } = "Yellow";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
