using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class GeneratedReport
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty; // HTML or structured summary

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string ReportType { get; set; } = "Monthly"; // Monthly, Daily, etc.

        public Guid? BranchId { get; set; } // Null for global/CEO report
        public Branch? Branch { get; set; }

        public string? GeneratedById { get; set; }
        public AppUser? GeneratedBy { get; set; }
    }
}
