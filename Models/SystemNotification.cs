using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class SystemNotification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "INFO"; // INFO, WARNING, CRITICAL, KPI

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public string? ActionUrl { get; set; }

        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string? Severity { get; set; } // red, yellow, blue, green
    }
}
