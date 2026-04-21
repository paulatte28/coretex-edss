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
            
            // Analytics for the widgets
            ViewBag.UserCount = await _userManager.Users.CountAsync();
            ViewBag.BranchCount = await _context.Branches.Where(b => !b.IsArchived).CountAsync();
            ViewBag.TotalRevenue = await _context.Sales.Where(s => !s.IsArchived).SumAsync(s => s.Amount);
            ViewBag.ForecastRevenue = await _analytics.GetSalesForecastAsync(branchId);

            // Backend #5: Strategic Alert Scanning
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            var highRiskList = new List<string>();
            foreach(var b in branches)
            {
                var rev = await _context.Sales.Where(s => !s.IsArchived && s.BranchId == b.Id).SumAsync(s => s.Amount);
                var exp = await _context.Expenses.Where(e => !e.IsArchived && e.BranchId == b.Id).SumAsync(e => e.Amount);
                if(rev > 0 && (exp/rev) > 0.8m) highRiskList.Add(b.Name);
            }
            ViewBag.HighRiskAlerts = highRiskList;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveBranch(Guid id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                branch.IsArchived = true;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_ARCHIVE", $"Archived branch: {branch.Name}");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, newRole.ToUpper());
                await _auditLog.LogActivityAsync("USER_ROLE_UPDATE", $"Changed role for {user.Email} to {newRole.ToUpper()}");
                TempData["Message"] = $"Role updated for {user.Email}.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        // --- Other Admin Actions ---

        [HttpPost]
        public async Task<IActionResult> SetGoal(Guid branchId, decimal targetRevenue, int month, int year)
        {
            var goal = new BranchGoal { BranchId = branchId, TargetRevenue = targetRevenue, Month = month, Year = year };
            _context.BranchGoals.Add(goal);
            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("GOAL_SET", $"Set strategic revenue goal for branch: {targetRevenue:C0}");
            return RedirectToAction(nameof(KPIThresholds));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGoal(Guid id)
        {
            var goal = await _context.BranchGoals.FindAsync(id);
            if (goal != null) { _context.BranchGoals.Remove(goal); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(KPIThresholds));
        }

        public async Task<IActionResult> KPIThresholds()
        {
            var goals = await _context.BranchGoals.Include(g => g.Branch).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            return View(goals);
        }
        public IActionResult GoalsTargets() => View();
        public async Task<IActionResult> ReportSchedule()
        {
            var summary = new BusinessSummaryViewModel
            {
                TotalRevenue = await _context.Sales.Where(s => !s.IsArchived).SumAsync(s => s.Amount),
                TotalExpenses = await _context.Expenses.Where(e => !e.IsArchived).SumAsync(e => e.Amount),
                ActiveBranches = await _context.Branches.Where(b => !b.IsArchived).CountAsync()
            };
            summary.NetProfit = summary.TotalRevenue - summary.TotalExpenses;
            return View(summary);
        }
        public async Task<IActionResult> ActivityLog()
        {
            var logs = await _context.ActivityLogs
                .Include(l => l.Branch)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }
        public async Task<IActionResult> BranchSubmissions()
        {
            var submissions = await _context.BranchSubmissions
                .Include(s => s.Branch)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            return View(submissions);
        }

        public async Task<IActionResult> AuditTrail()
        {
            var logs = await _context.ActivityLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }
    }
}

