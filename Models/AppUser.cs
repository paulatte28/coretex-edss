using System;
using Microsoft.AspNetCore.Identity;

namespace coretex_finalproj.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        
        // Logical separation for Multi-Tenancy
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }
    }
}
