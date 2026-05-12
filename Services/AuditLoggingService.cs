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

        public async Task LogActivityAsync(string actionType, string description, Guid? explicitBranchId = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userName = user?.Identity?.Name ?? "Anonymous";
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Guid branchId = Guid.Empty;

            // Use explicit branch ID if provided (e.g. CEO setting target for a specific branch)
            if (explicitBranchId.HasValue && explicitBranchId.Value != Guid.Empty)
            {
                branchId = explicitBranchId.Value;
            }
            else
            {
                // Auto-detect from claims
                var branchIdClaim = user?.FindFirstValue("BranchId");
                if (!string.IsNullOrEmpty(branchIdClaim) && Guid.TryParse(branchIdClaim, out Guid bId))
                {
                    branchId = bId;
                }
            }

            // Fallback for global actions
            if (branchId == Guid.Empty)
            {
                 var branch = _context.Branches.FirstOrDefault(b => b.BranchCode == "MAIN") 
                            ?? _context.Branches.FirstOrDefault();
                 if (branch != null) branchId = branch.Id;
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
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
