using coretex_finalproj.Data;
using coretex_finalproj.Models;
using System.Security.Claims;

namespace coretex_finalproj.Services
{
    public class AuditLoggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLoggingService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string actionType, string description)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userName = user?.Identity?.Name ?? "Anonymous";
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Try to find BranchId from claims
            var branchIdClaim = user?.FindFirstValue("BranchId");
            Guid branchId = Guid.Empty;
            if (Guid.TryParse(branchIdClaim, out Guid bId))
            {
                branchId = bId;
            }

            // Fallback to Main Branch if not set and user is authenticated
            if (branchId == Guid.Empty && user?.Identity?.IsAuthenticated == true)
            {
                 var mainBranch = _context.Branches.FirstOrDefault(b => b.BranchCode == "MAIN");
                 if (mainBranch != null) branchId = mainBranch.Id;
            }

            var role = user?.FindFirstValue(ClaimTypes.Role) ?? "Member";

            var log = new ActivityLogEntry
            {
                UserId = userId,
                UserName = userName,
                UserRole = role,
                ActionType = actionType,
                Description = description,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
