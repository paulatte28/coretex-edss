using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CASHIER,ADMIN,CEO,FINANCE,BRANCH_ADMIN")]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly coretex_finalproj.Data.ApplicationDbContext _context;

        public ProfileController(UserManager<AppUser> userManager, IWebHostEnvironment environment, coretex_finalproj.Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _environment = environment;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // STALE SESSION DETECTED: User exists in cookie but not in DB (likely due to Seeder purge)
                var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<AppUser>>();
                await signInManager.SignOutAsync();
                return RedirectToAction("Login", "Home");
            }

            // Explicitly load branch to avoid EF Core shadowing issues
            if (user.BranchId.HasValue)
            {
                user.Branch = await _context.Branches.FindAsync(user.BranchId.Value);
            }

            ViewBag.RecentActivity = _context.ActivityLogs
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToList();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                // LOG TO CEO AUDIT TRAIL
                var auditLog = (AuditLoggingService)HttpContext.RequestServices.GetRequiredService(typeof(AuditLoggingService));
                await auditLog.LogActivityAsync("SECURITY_IDENTITY_UPDATE", $"User {user.Email} updated their personal authentication credentials.");
                
                TempData["Success"] = "SECURITY SYNCHRONIZED: Password updated and logged to audit trail.";
            }
            else
            {
                TempData["Error"] = "AUTHENTICATION FAILED: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar != null && avatar.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{user.Id}_{avatar.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.ProfilePicturePath = $"/uploads/avatars/{fileName}";
                await _userManager.UpdateAsync(user);

                TempData["Success"] = "Avatar updated!";
            }
            return RedirectToAction("Index");
        }
    }
}
