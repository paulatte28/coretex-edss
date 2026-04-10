using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class GoalTarget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string MetricName { get; set; } = string.Empty;

        public decimal TargetValue { get; set; }

        [Required]
        [StringLength(20)]
        public string PeriodType { get; set; } = "Monthly";

        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
