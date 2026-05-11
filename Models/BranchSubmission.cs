using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class BranchSubmission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }

        [Range(2000, 2100)]
        public int SubmissionYear { get; set; }

        [Range(1, 12)]
        public int SubmissionMonth { get; set; }

        [StringLength(450)]
        public string? SubmittedByUserId { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Required]
        [Range(0, 999999999)]
        public decimal SalesRevenue { get; set; }

        [Required]
        [Range(0, 999999999)]
        public decimal Expenses { get; set; }

        // Itemized Breakdown for EDSS Analytics
        public decimal Cogs { get; set; }
        public decimal Rent { get; set; }
        public decimal Salaries { get; set; }
        public decimal Utilities { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
