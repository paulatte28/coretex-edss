using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class ActivityLogEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
