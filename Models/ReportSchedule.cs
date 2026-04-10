using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class ReportSchedule
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool IsEnabled { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string Frequency { get; set; } = "Weekly";

        public int? DayOfWeek { get; set; }

        public TimeSpan ScheduledTime { get; set; } = new TimeSpan(8, 0, 0);

        [Required]
        public string Recipients { get; set; } = string.Empty;

        [Required]
        public string ReportTypes { get; set; } = "Executive";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
