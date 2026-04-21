using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class BranchGoal
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }

        [Required]
        public decimal TargetRevenue { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        public bool IsNotified { get; set; } = false;
    }
}
