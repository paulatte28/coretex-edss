using coretex_finalproj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace coretex_finalproj.Controllers
{
    [Authorize]
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
            if (user == null) return RedirectToAction("Login", "Home");

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
                TempData["Success"] = "Password changed successfully!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
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
