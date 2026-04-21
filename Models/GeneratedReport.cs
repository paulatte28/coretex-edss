using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coretex_finalproj.Models
{
    public class GeneratedReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string PeriodLabel { get; set; } = string.Empty;

        [Required]
        public string ReportHtml { get; set; } = string.Empty;

        public string SentToEmail { get; set; } = string.Empty;

        public string SummaryData { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        // Relationship fixed: changed from int? to Guid? to match Branch model
        public Guid? BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public string? GeneratedById { get; set; }
        [ForeignKey("GeneratedById")]
        public virtual AppUser? GeneratedBy { get; set; }
    }
}
