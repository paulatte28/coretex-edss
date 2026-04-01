using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Tenant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        public string PlanType { get; set; } = "Basic"; // Basic, Premium, Enterprise

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active"; // Active, Suspended, Deactivated
    }
}
