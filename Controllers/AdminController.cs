using coretex_finalproj.Data;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly AnalyticsService _analytics;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditLoggingService _auditLog;

        public AdminController(
            AnalyticsService analytics, 
            ApplicationDbContext context, 
            UserManager<AppUser> userManager,
            AuditLoggingService auditLog)
        {
            _analytics = analytics;
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
        }

        // Admin Dashboard / Overview
        public async Task<IActionResult> Index(Guid? branchId)
        {
            ViewBag.SelectedBranchId = branchId;
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync(branchId);
            ViewBag.BranchData = await _analytics.GetBranchPerformanceAsync();
            ViewBag.ExpenseData = await _analytics.GetExpenseCategoriesAsync(branchId);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyAnalytics(Guid? branchId) => Json(await _analytics.GetMonthlyProfitLossAsync(branchId));

        [HttpGet]
        public async Task<IActionResult> GetBranchAnalytics() => Json(await _analytics.GetBranchPerformanceAsync());

        [HttpGet]
        public async Task<IActionResult> GetExpenseAnalytics(Guid? branchId) => Json(await _analytics.GetExpenseCategoriesAsync(branchId));

        // --- Branch Management ---

        public async Task<IActionResult> Pos()
        {
            var sales = await _context.Sales.Where(s => s.Date >= DateTime.Today && !s.IsArchived).ToListAsync();
            return View(sales);
        }

        public async Task<IActionResult> BranchManagement()
        {
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            return View(branches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch(Branch branch)
        {
            // Check for duplicate branch code
            if (await _context.Branches.AnyAsync(b => b.BranchCode == branch.BranchCode))
            {
                ModelState.AddModelError("BranchCode", "This Branch Code already exists.");
            }

            if (ModelState.IsValid)
            {
                branch.Id = Guid.NewGuid();
                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_CREATE", $"Created branch: {branch.Name} ({branch.BranchCode})");
                return RedirectToAction(nameof(BranchManagement));
            }
            return View(nameof(BranchManagement), await _context.Branches.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBranchStatus(Guid id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                branch.IsActive = !branch.IsActive;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_STATUS_TOGGLE", $"Toggled status for branch {branch.Name} to {(branch.IsActive ? "Active" : "Inactive")}");
            }
            return RedirectToAction(nameof(BranchManagement));
        }

        // --- User Management ---

        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users.Include(u => u.Branch).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string fullName, string role, Guid? branchId, string password)
        {
            // Check if user already exists
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError("", "A user with this email already exists.");
            }
            else
            {
                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    BranchId = branchId,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role.ToUpper());
                    await _auditLog.LogActivityAsync("USER_CREATE", $"Created user {email} with role {role.ToUpper()}");
                    return RedirectToAction(nameof(UserManagement));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var users = await _userManager.Users.Include(u => u.Branch).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            return View(nameof(UserManagement), users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (result.Succeeded)
                {
                    await _auditLog.LogActivityAsync("USER_PASSWORD_RESET", $"Reset password for user {user.Email}");
                    TempData["Message"] = $"Password for {user.Email} has been reset.";
                }
            }
            return RedirectToAction(nameof(UserManagement));
        }

        // --- Other Admin Actions ---

        public IActionResult KPIThresholds() => View();
        public IActionResult GoalsTargets() => View();
        public IActionResult ReportSchedule() => View();
        public async Task<IActionResult> ActivityLog()
        {
            var logs = await _context.ActivityLogs
                .Include(l => l.Branch)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }
        public IActionResult BranchSubmissions() => View();
        public IActionResult AuditTrail() => View();
    }
}

