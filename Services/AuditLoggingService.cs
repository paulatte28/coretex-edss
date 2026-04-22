using coretex_finalproj.Data;
using coretex_finalproj.Models;
using System.Security.Claims;

namespace coretex_finalproj.Services
{
    public class AuditLoggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly GeolocationService _geoService;

        public AuditLoggingService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, GeolocationService geoService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _geoService = geoService;
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
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var geo = await _geoService.GetLocationAsync(ipAddress);

            var log = new ActivityLogEntry
            {
                UserId = userId,
                UserName = userName,
                UserRole = role,
                ActionType = actionType,
                Description = description,
                IpAddress = ipAddress,
                Location = $"{geo.City}, {geo.CountryName}",
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
