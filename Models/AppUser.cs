using System;
using Microsoft.AspNetCore.Identity;

namespace coretex_finalproj.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public Guid? BranchId { get; set; }

        public Branch? Branch { get; set; }
        public string? ProfilePicturePath { get; set; }
        
        // Security Tracking
        public string? LastLoginLocation { get; set; }
        public string? LastLoginIP { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
