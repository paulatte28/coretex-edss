using coretex_finalproj.Data;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "ADMIN,BRANCH_ADMIN,CEO,FINANCE,CASHIER")]
    public class AdminController(
        AnalyticsService analytics,
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        AuditLoggingService auditLog,
        IEmailSender emailSender,
        IWebHostEnvironment webHostEnvironment,
        NotificationService notificationService) : Controller
    {
        private readonly AnalyticsService _analytics = analytics;
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly AuditLoggingService _auditLog = auditLog;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender = emailSender;
        private readonly IWebHostEnvironment _env = webHostEnvironment;
        private readonly NotificationService _notificationService = notificationService;

        // Admin Dashboard / Overview
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Home");

            var isManager = await _userManager.IsInRoleAsync(currentUser, "BRANCH_ADMIN");
            var branchId = currentUser?.BranchId;

            if (isManager && branchId != null)
            {
                // --- BRANCH MANAGER VIEW ---
                var branch = await _context.Branches.FindAsync(branchId);
                ViewBag.BranchName = branch?.Name ?? "Assigned Branch";

                ViewBag.UserCount = await _userManager.Users.CountAsync(u => u.BranchId == branchId);
                ViewBag.BranchCount = 1; // Only their branch

                var dayAgo = DateTime.UtcNow.AddDays(-1);
                var today = DateTime.UtcNow.Date;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                ViewBag.SecurityEventsCount = await _context.ActivityLogs
                    .CountAsync(l => l.BranchId == branchId && l.CreatedAt >= dayAgo);

                // Sales & Expenses for this branch
                ViewBag.TodaySales = await _context.Sales
                    .Where(s => s.BranchId == branchId && !s.IsArchived && s.Date >= today)
                    .SumAsync(s => s.Amount);

                ViewBag.MonthSales = await _context.Sales
                    .Where(s => s.BranchId == branchId && !s.IsArchived && s.Date >= monthStart)
                    .SumAsync(s => s.Amount);

                ViewBag.TodayExpenses = await _context.Expenses
                    .Where(e => e.BranchId == branchId && !e.IsArchived && e.Date >= today)
                    .SumAsync(e => e.Amount);

                ViewBag.MonthExpenses = await _context.Expenses
                    .Where(e => e.BranchId == branchId && !e.IsArchived && e.Date >= monthStart)
                    .SumAsync(e => e.Amount);

                // Pending submissions for THIS branch
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var hasSubmitted = await _context.BranchSubmissions
                    .AnyAsync(s => s.BranchId == branchId && s.SubmittedAt.Month == currentMonth && s.SubmittedAt.Year == currentYear);

                ViewBag.PendingSubmissions = hasSubmitted ? 0 : 1;
                ViewBag.KPIThresholdsCount = await _context.KpiThresholds.CountAsync(t => t.BranchId == branchId);
            }
            else
            {
                // --- SYSTEM ADMIN VIEW ---
                var today = DateTime.UtcNow.Date;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                ViewBag.UserCount = await _userManager.Users.CountAsync();
                ViewBag.BranchCount = await _context.Branches.Where(b => !b.IsArchived).CountAsync();

                var dayAgo = DateTime.UtcNow.AddDays(-1);
                ViewBag.SecurityEventsCount = await _context.ActivityLogs
                    .CountAsync(l => l.CreatedAt >= dayAgo);

                // Global Stats
                ViewBag.TodaySales = await _context.Sales
                    .Where(s => !s.IsArchived && s.Date >= today)
                    .SumAsync(s => s.Amount);

                ViewBag.MonthSales = await _context.Sales
                    .Where(s => !s.IsArchived && s.Date >= monthStart)
                    .SumAsync(s => s.Amount);

                ViewBag.TodayExpenses = await _context.Expenses
                    .Where(e => !e.IsArchived && e.Date >= today)
                    .SumAsync(e => e.Amount);

                ViewBag.MonthExpenses = await _context.Expenses
                    .Where(e => !e.IsArchived && e.Date >= monthStart)
                    .SumAsync(e => e.Amount);

                // System Infrastructure Metrics
                var activeBranchCount = await _context.Branches.CountAsync(b => b.IsActive && !b.IsArchived);
                
                ViewBag.ActiveNodes = activeBranchCount;
                ViewBag.TotalStaff = await _userManager.Users.CountAsync();
                ViewBag.SystemUptime = "99.98%";
                ViewBag.DatabaseHealth = "Optimal";
            }

            // Recent Activity (Filtered if Manager)
            IQueryable<ActivityLogEntry> logQuery = _context.ActivityLogs.Include(l => l.Branch);
            if (isManager && branchId != null) logQuery = logQuery.Where(l => l.BranchId == branchId);

            ViewBag.RecentActivity = await logQuery
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

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

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> BranchManagement()
        {
            // Load both active and archived branches for integrated management
            var branches = await _context.Branches.ToListAsync();
            return View(branches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
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
        [Authorize(Roles = "ADMIN")]
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
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ArchiveBranch(Guid id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                branch.IsArchived = true;
                
                // ATOMIC DEACTIVATION of all staff in this branch
                var branchStaff = await _context.Users.Where(u => u.BranchId == id).ToListAsync();
                foreach (var s in branchStaff)
                {
                    s.IsActive = false;
                }

                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_ARCHIVE", $"Archived branch: {branch.Name}. {branchStaff.Count} staff members deactivated.");
            }
            return RedirectToAction(nameof(BranchManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateBranch(Branch branch)
        {
            var existing = await _context.Branches.FindAsync(branch.Id);
            if (existing != null)
            {
                existing.Name = branch.Name;
                existing.BranchCode = branch.BranchCode;
                existing.Address = branch.Address;

                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_UPDATE", $"Updated branch details for: {branch.Name}");
                TempData["Message"] = $"Branch '{branch.Name}' updated successfully.";
            }
            return RedirectToAction(nameof(BranchManagement));
        }

        // --- User Management ---

        public async Task<IActionResult> UserManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            // Load both active and inactive users for integrated management
            IQueryable<AppUser> query = _userManager.Users.Include(u => u.Branch);

            if (currentUser?.BranchId != null)
            {
                // Branch Admin view: Filter by branch and HIDE self to avoid redundancy
                query = query.Where(u => u.BranchId == currentUser.BranchId && u.Id != currentUser.Id);
                ViewBag.Branches = await _context.Branches.Where(b => b.Id == currentUser.BranchId).ToListAsync();
            }
            else
            {
                // Global Admin view
                ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            }

            // GLOBAL AUDIT: Detect filled roles per branch for dynamic UI locking
            var branchOccupancy = await _context.Users
                .Where(u => u.BranchId != null && (u.Role == "BRANCH_ADMIN" || u.Role == "FINANCE"))
                .GroupBy(u => u.BranchId)
                .Select(g => new { 
                    BranchId = g.Key, 
                    Roles = g.Select(u => u.Role).ToList() 
                })
                .ToDictionaryAsync(x => x.BranchId!.Value, x => x.Roles);
            
            ViewBag.BranchOccupancy = branchOccupancy;

            var users = await query.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string fullName, string role, Guid? branchId)
        {
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["Error"] = "PROVISIONING FAILED: Email already exists.";
                return RedirectToAction(nameof(UserManagement));
            }

            // SOP ENFORCEMENT: Role-Based Quotas per branch
            if (branchId.HasValue)
            {
                var upperRole = role.ToUpper();
                if (upperRole == "BRANCH_ADMIN" || upperRole == "FINANCE")
                {
                    var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.BranchId == branchId && u.Role == upperRole);
                    if (existing != null)
                    {
                        var roleName = upperRole == "BRANCH_ADMIN" ? "Manager" : "Finance Officer";
                        TempData["Error"] = $"SOP VIOLATION: This branch already has an active {roleName} ({existing.FullName}). Personnel quota for this role is strictly 1 per node.";
                        return RedirectToAction(nameof(UserManagement));
                    }
                }
            }

            // ENHANCED: Professional Password Generation (Following Project Convention)
            string tempPassword;
            var branch = branchId.HasValue ? await _context.Branches.FindAsync(branchId.Value) : null;

            if (branch != null)
            {
                var branchClean = branch.Name.ToLower().Replace(" branch", "").Replace(" ", "");
                var branchPascal = char.ToUpper(branchClean[0]) + branchClean[1..];
                var rolePascal = char.ToUpper(role[0]) + role[1..].ToLower().Split('_')[0];
                tempPassword = $"{rolePascal}{branchPascal}123!";
            }
            else
            {
                var rolePascal = char.ToUpper(role[0]) + role[1..].ToLower().Split('_')[0];
                tempPassword = $"{rolePascal}12345!";
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                BranchId = branchId,
                Role = role.ToUpper(),
                EmailConfirmed = true,
                TwoFactorEnabled = true
            };

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role.ToUpper());

                string emailBody = $@"
                    <h2>Welcome to CORETEX</h2>
                    <p>Hello {fullName}, your account is ready.</p>
                    <p>Temporary Password: <b>{tempPassword}</b></p>
                    <p>Please change this after your first login.</p>";

                await _emailSender.SendEmailAsync(email, "Account Provisioned - CORETEX Security", emailBody);

                await _auditLog.LogActivityAsync("USER_PROVISION", $"Provisioned {email} with auto-gen password.");
                TempData["Message"] = $"SUCCESS: User {email} provisioned and emailed.";
                return RedirectToAction(nameof(UserManagement));
            }

            TempData["Error"] = "ERROR: " + string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(string userId, string fullName, string role, bool isActive = false)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isTargetCeo = await _userManager.IsInRoleAsync(user, "CEO");

            if (isTargetCeo && !User.IsInRole("CEO"))
            {
                await _auditLog.LogActivityAsync("SECURITY_VIOLATION", $"Admin {currentUser?.Email} attempted to modify CEO {user.Email}. Action Blocked.");
                TempData["Error"] = "SECURITY VIOLATION: You do not have the authority to modify the CEO's security profile.";
                return RedirectToAction(nameof(UserManagement));
            }

            var oldRole = user.Role;
            var oldStatus = user.IsActive;

            user.FullName = fullName;
            user.Role = role.ToUpper();
            user.IsActive = isActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (oldRole != user.Role) await _auditLog.LogActivityAsync("SECURITY_PROMOTION", $"CRITICAL: {user.Email} role changed from {oldRole} to {user.Role}.");
                if (oldStatus != user.IsActive) await _auditLog.LogActivityAsync("USER_STATUS_CHANGE", $"User {user.Email} access {(user.IsActive ? "Enabled" : "Disabled")}.");
                await _auditLog.LogActivityAsync("USER_UPDATE", $"Personnel record updated: {user.Email}");
                TempData["Message"] = "Personnel records updated successfully.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Professional Reset Workflow
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, "CoretexReset123!");

            if (result.Succeeded)
            {
                await _auditLog.LogActivityAsync("SECURITY_RESET", $"Manager forced password reset for {user.Email} to default.");
                return Ok();
            }

            return BadRequest("Reset failed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "SECURITY ALERT: You cannot terminate your own administrative access.";
                return RedirectToAction(nameof(UserManagement));
            }

            // --- SOFT DELETE (ARCHIVING) ---
            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                await _auditLog.LogActivityAsync("USER_ARCHIVE", $"Archived personnel access for {user.Email}.");
                TempData["Message"] = "Personnel access revoked and archived successfully.";
            }
            else
            {
                TempData["Error"] = "Termination failed.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var isTargetCeo = await _userManager.IsInRoleAsync(user, "CEO");
            if (isTargetCeo && !User.IsInRole("CEO"))
            {
                TempData["Error"] = "SECURITY VIOLATION: Password reset for CEO is restricted.";
                return RedirectToAction(nameof(UserManagement));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                await _auditLog.LogActivityAsync("USER_PASSWORD_RESET", $"Security credentials reset for: {user.Email}");
                TempData["Message"] = $"Password for {user.Email} has been reset.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var isTargetCeo = await _userManager.IsInRoleAsync(user, "CEO");
            if (isTargetCeo && !User.IsInRole("CEO"))
            {
                TempData["Error"] = "SECURITY VIOLATION: You cannot deactivate the CEO account.";
                return RedirectToAction(nameof(UserManagement));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _auditLog.LogActivityAsync("USER_STATUS_TOGGLE", $"Account {(user.IsActive ? "Activated" : "Deactivated")}: {user.Email}");
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction(nameof(UserManagement));

            // CRITICAL SECURITY: Only a CEO can promote someone to CEO
            if (newRole.Equals("CEO", StringComparison.OrdinalIgnoreCase) && !User.IsInRole("CEO"))
            {
                TempData["Error"] = "AUTHORIZATION DENIED: Promoting to CEO/Executive requires Owner-level credentials. Please contact the Business Owner.";
                return RedirectToAction(nameof(UserManagement));
            }

            // SECURITY GUARD LOGIC: Check if we are demoting the only CEO
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("CEO") && !newRole.Equals("CEO", StringComparison.OrdinalIgnoreCase))
            {
                var allCeos = await _userManager.GetUsersInRoleAsync("CEO");
                if (allCeos.Count <= 1)
                {
                    TempData["Error"] = "SECURITY LOCK: Cannot demote the last remaining CEO. System must have at least one executive lead.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole.ToUpper());
            await _auditLog.LogActivityAsync("SECURITY_EVENT", $"Admin manually overrode role for {user.Email} to {newRole.ToUpper()}");

            TempData["Message"] = $"Authorization updated for {user.Email}.";
            return RedirectToAction(nameof(UserManagement));
        }

        // --- Other Admin Actions ---

        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> SetGoal(Guid branchId, decimal targetRevenue, int month, int year)
        {
            if (branchId == Guid.Empty)
            {
                // Fallback: If no branch is selected, assign to the first active branch
                var firstBranch = await _context.Branches.FirstOrDefaultAsync(b => !b.IsArchived);
                if (firstBranch == null) return RedirectToAction(nameof(KPIThresholds));
                branchId = firstBranch.Id;
            }

            var goal = new BranchGoal { BranchId = branchId, TargetRevenue = targetRevenue, Month = month, Year = year };
            _context.BranchGoals.Add(goal);
            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("GOAL_SET", $"Set strategic revenue goal for branch: {targetRevenue:C0}");
            return RedirectToAction(nameof(KPIThresholds));
        }

        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> UpdateGoal(Guid id, Guid branchId, decimal targetRevenue, int month, int year)
        {
            var goal = await _context.BranchGoals.FindAsync(id);
            if (goal != null)
            {
                goal.BranchId = branchId;
                goal.TargetRevenue = targetRevenue;
                goal.Month = month;
                goal.Year = year;

                _context.BranchGoals.Update(goal);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("GOAL_UPDATE", $"Updated strategic revenue goal for branch to: {targetRevenue:C0}");
            }
            return RedirectToAction(nameof(KPIThresholds));
        }

        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> DeleteGoal(Guid id)
        {
            var goal = await _context.BranchGoals.FindAsync(id);
            if (goal != null) { _context.BranchGoals.Remove(goal); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(KPIThresholds));
        }

        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> KPIThresholds()
        {
            var goals = await _context.BranchGoals.Include(g => g.Branch).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            
            // Fetch live threshold alerts from the database
            ViewBag.Notifications = await _context.SystemNotifications
                .Where(n => n.Type == "KPI" || n.Type == "CRITICAL" || n.Type == "WARNING")
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(goals);
        }
        public async Task<IActionResult> GoalsTargets()
        {
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            return View();
        }
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ReportSchedule()
        {
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived && b.IsActive).ToListAsync();
            ViewBag.CeoUsers = await _userManager.GetUsersInRoleAsync("CEO");
            
            var summary = new BusinessSummaryViewModel
            {
                TotalRevenue = await _context.Sales.Where(s => !s.IsArchived).SumAsync(s => s.Amount),
                TotalExpenses = await _context.Expenses.Where(e => !e.IsArchived).SumAsync(e => e.Amount),
                ActiveBranches = await _context.Branches.Where(b => !b.IsArchived).CountAsync(),
                CurrentSchedule = await _context.ReportSchedules.FirstOrDefaultAsync()
            };
            summary.NetProfit = summary.TotalRevenue - summary.TotalExpenses;
            return View(summary);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SaveReportSchedule(bool enabled, string frequency, int? dayOfWeek, string time, string recipients, string reportTypes)
        {
            var hqBranch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchCode == "CORETEX-HQ") 
                           ?? await _context.Branches.FirstOrDefaultAsync();

            if (hqBranch == null) return BadRequest("No branches found to associate schedule.");

            var schedule = await _context.ReportSchedules.FirstOrDefaultAsync() ?? new ReportSchedule { BranchId = hqBranch.Id };
            
            schedule.IsEnabled = enabled;
            schedule.Frequency = frequency;
            schedule.DayOfWeek = dayOfWeek;
            schedule.ScheduledTime = TimeSpan.Parse(time);
            schedule.Recipients = recipients;
            schedule.ReportTypes = reportTypes;
            schedule.UpdatedAt = DateTime.UtcNow;

            if (_context.Entry(schedule).State == EntityState.Detached)
            {
                _context.ReportSchedules.Add(schedule);
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("REPORT_SCHEDULE_UPDATED", $"Auto-reports {(enabled ? "Enabled" : "Disabled")}. Frequency: {frequency}");

            return Json(new { success = true, message = "Schedule saved successfully!" });
        }
        public async Task<IActionResult> ActivityLog()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<AppUser>>();
                await signInManager.SignOutAsync();
                return RedirectToAction("Login", "Home");
            }

            IQueryable<ActivityLogEntry> query = _context.ActivityLogs.Include(l => l.Branch);

            // ROLE-SPECIFIC SCOPING: Branch Managers and Finance see only their OWN logs (My Audit Logs)
            if (User.IsInRole("BRANCH_ADMIN") || User.IsInRole("FINANCE") || User.IsInRole("CASHIER"))
            {
                query = query.Where(l => l.UserName == currentUser.UserName);
            }
            // ADMIN ISOLATION: Admins see their branch-wide logs unless they are global admins
            else if (currentUser?.BranchId != null && !User.IsInRole("CEO") && !User.IsInRole("ADMIN"))
            {
                query = query.Where(l => l.BranchId == currentUser.BranchId);
            }

            // FOR GLOBAL OVERSIGHT: Provide branch list for filtering
            if (User.IsInRole("ADMIN") || User.IsInRole("CEO"))
            {
                ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            }

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }
        public async Task<IActionResult> BranchSubmissions()
        {
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            var submissions = await _context.BranchSubmissions
                .Include(s => s.Branch)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            return View(submissions);
        }

        // Removed AuditTrail from Admin scope (Moved to CEO for SoD Compliance)
        // --- Strategic Goals & KPI Backend ---

        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> SaveKpiThreshold([FromBody] KpiThresholdRequest request)
        {
            if (request == null) return BadRequest();

            var branchesToUpdate = new List<Guid>();
            if (request.BranchId == "global")
            {
                branchesToUpdate = await _context.Branches.Where(b => !b.IsArchived).Select(b => b.Id).ToListAsync();
            }
            else if (Guid.TryParse(request.BranchId, out Guid bId))
            {
                branchesToUpdate.Add(bId);
            }

            if (!branchesToUpdate.Any()) return BadRequest("No valid branch target selected.");

            bool anyAlertTriggered = false;

            foreach (var bId in branchesToUpdate)
            {
                var existing = await _context.KpiThresholds
                    .FirstOrDefaultAsync(t => t.BranchId == bId && t.IsActive);

                if (existing != null)
                {
                    existing.MinProfitMargin = request.MinProfitMargin;
                    existing.MaxExpenseRatio = request.MaxExpenseRatio;
                    existing.MinMonthlyProfit = request.MinMonthlyProfit;
                    existing.RiskAlertLevel = request.RiskAlertLevel;
                    existing.UpdatedAt = DateTime.Now;
                    _context.KpiThresholds.Update(existing);
                }
                else
                {
                    _context.KpiThresholds.Add(new KpiThreshold
                    {
                        BranchId = bId,
                        MinProfitMargin = request.MinProfitMargin,
                        MaxExpenseRatio = request.MaxExpenseRatio,
                        MinMonthlyProfit = request.MinMonthlyProfit,
                        RiskAlertLevel = request.RiskAlertLevel,
                        IsActive = true
                    });
                }

                // Check for immediate strategic breach
                var snapshot = await _analytics.GetDashboardSnapshotAsync(bId);
                if (snapshot.ProfitMargin < request.MinProfitMargin)
                {
                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = "Strategic Breach Detected",
                        Message = $"Branch operational health ({snapshot.ProfitMargin:F1}%) is below your new global target ({request.MinProfitMargin:F1}%).",
                        Type = "KPI",
                        Severity = "red",
                        BranchId = bId,
                        CreatedAt = DateTime.UtcNow
                    });
                    anyAlertTriggered = true;
                }
            }

            await _context.SaveChangesAsync();
            string logMsg = request.BranchId == "global" 
                ? "Deployed global KPI safety thresholds across all active branches."
                : $"Updated strategic KPI thresholds for a specific branch node.";
            
            await _auditLog.LogActivityAsync("KPI_CONFIG", logMsg);

            return Json(new { success = true, alerted = anyAlertTriggered });
        }

        [HttpGet]
        public async Task<IActionResult> GetKpiThreshold(string branchId)
        {
            if (string.IsNullOrEmpty(branchId)) return BadRequest();

            KpiThreshold? threshold = null;

            if (branchId == "global")
            {
                threshold = await _context.KpiThresholds
                    .OrderByDescending(t => t.UpdatedAt)
                    .FirstOrDefaultAsync();
            }
            else if (Guid.TryParse(branchId, out Guid bId))
            {
                threshold = await _context.KpiThresholds
                    .FirstOrDefaultAsync(t => t.BranchId == bId && t.IsActive);
                
                // Fallback: If this branch has no settings, show the latest global settings
                if (threshold == null)
                {
                    threshold = await _context.KpiThresholds
                        .OrderByDescending(t => t.UpdatedAt)
                        .FirstOrDefaultAsync();
                }
            }

            return Json(threshold);
        }

        public class KpiThresholdRequest
        {
            public string BranchId { get; set; } = string.Empty;
            public decimal MinProfitMargin { get; set; }
            public decimal MaxExpenseRatio { get; set; }
            public decimal MinMonthlyProfit { get; set; }
            public string RiskAlertLevel { get; set; } = "Yellow";
        }

        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> SaveGoalTarget([FromBody] GoalTarget goal)
        {
            if (goal == null) return BadRequest();

            if (goal.Id == Guid.Empty) goal.Id = Guid.NewGuid();

            var existing = await _context.GoalTargets.FindAsync(goal.Id);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(goal);
            }
            else
            {
                _context.GoalTargets.Add(goal);
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("GOAL_CREATE", $"Set strategic {goal.MetricName} target: {goal.TargetValue}", goal.BranchId);
            return Json(new { success = true, id = goal.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetGoalTargets(Guid? branchId)
        {
            var query = _context.GoalTargets.AsQueryable();
            if (branchId.HasValue) query = query.Where(g => g.BranchId == branchId.Value);
            return Json(await query.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGoalTarget(Guid id)
        {
            var goal = await _context.GoalTargets.FindAsync(id);
            if (goal == null) return NotFound();
            _context.GoalTargets.Remove(goal);
            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("GOAL_DELETE", $"Deleted strategic goal: {goal.MetricName}");
            return Json(new { success = true });
        }

        // --- Archive Management ---

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Archives()
        {
            var backupsDir = Path.Combine(_env.WebRootPath, "backups");
            if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);

            var backupFiles = new DirectoryInfo(backupsDir)
                .GetFiles("*.snap")
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFile
                {
                    FileName = f.Name,
                    SizeBytes = f.Length,
                    CreatedAt = f.CreationTime
                }).ToList();

            var model = new ArchivesViewModel
            {
                ArchivedBranches = await _context.Branches.Where(b => b.IsArchived).ToListAsync(),
                ArchivedStaff = await _userManager.Users.Where(u => !u.IsActive).ToListAsync(),
                SystemBackups = backupFiles
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GenerateSnapshot()
        {
            try
            {
                var snapshot = new
                {
                    Branches = await _context.Branches.ToListAsync(),
                    Sales = await _context.Sales.ToListAsync(),
                    Expenses = await _context.Expenses.ToListAsync(),
                    Users = await _context.Users.Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.BranchId, u.Role }).ToListAsync(),
                    ActivityLogs = await _context.ActivityLogs.OrderByDescending(l => l.CreatedAt).Take(500).ToListAsync(),
                    GoalTargets = await _context.GoalTargets.ToListAsync(),
                    KpiThresholds = await _context.KpiThresholds.ToListAsync(),
                    Timestamp = DateTime.UtcNow,
                    SystemVersion = "2.1.0"
                };

                string fileName = $"Coretex_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.snap";
                string json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                await _auditLog.LogActivityAsync("SYSTEM_SNAPSHOT_GEN", $"Generated portable system snapshot: {fileName}");
                
                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Snapshot generation failed: {ex.Message}";
                return RedirectToAction(nameof(Archives));
            }
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UploadAndRestore(IFormFile snapshotFile)
        {
            if (snapshotFile == null || snapshotFile.Length == 0)
            {
                TempData["Error"] = "No snapshot file selected.";
                return RedirectToAction(nameof(Archives));
            }

            try
            {
                using var reader = new StreamReader(snapshotFile.OpenReadStream());
                string json = await reader.ReadToEndAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<SnapshotData>(json);

                if (data == null)
                {
                    TempData["Error"] = "Invalid snapshot format.";
                    return RedirectToAction(nameof(Archives));
                }

                // --- CAUTION: DESTRUCTIVE OPERATION ---
                _context.Sales.RemoveRange(_context.Sales);
                _context.Expenses.RemoveRange(_context.Expenses);
                _context.GoalTargets.RemoveRange(_context.GoalTargets);
                _context.KpiThresholds.RemoveRange(_context.KpiThresholds);
                _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
                _context.Branches.RemoveRange(_context.Branches);
                
                await _context.SaveChangesAsync();

                // Re-insert data
                if (data.Branches != null) _context.Branches.AddRange(data.Branches);
                if (data.Sales != null) _context.Sales.AddRange(data.Sales);
                if (data.Expenses != null) _context.Expenses.AddRange(data.Expenses);
                if (data.GoalTargets != null) _context.GoalTargets.AddRange(data.GoalTargets);
                if (data.KpiThresholds != null) _context.KpiThresholds.AddRange(data.KpiThresholds);
                if (data.ActivityLogs != null) _context.ActivityLogs.AddRange(data.ActivityLogs);
                
                // Handle Personnel Restoration (Selective to avoid self-lockout)
                if (data.Users != null)
                {
                    var currentUserId = _userManager.GetUserId(User);
                    foreach (var u in data.Users)
                    {
                        if (u.Id == currentUserId) continue; // Safety skip
                        
                        var existingUser = await _userManager.FindByIdAsync(u.Id);
                        if (existingUser == null)
                        {
                            var newUser = new AppUser
                            {
                                Id = u.Id,
                                UserName = u.UserName ?? "User_" + Guid.NewGuid().ToString().Substring(0,8),
                                Email = u.Email ?? "no-email@coretex.com",
                                FullName = u.FullName ?? "System User",
                                BranchId = u.BranchId,
                                Role = u.Role ?? "CASHIER",
                                IsActive = false 
                            };
                            await _userManager.CreateAsync(newUser, "TempPass123!");
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SYSTEM_RESTORE_UPLOAD", $"Full system recovery completed via file upload: {snapshotFile.FileName}");

                TempData["Message"] = "System successfully rebuilt! Database and Personnel recovered.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Restoration failed: {ex.Message}";
            }
            return RedirectToAction(nameof(Archives));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RestoreSnapshot(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_env.WebRootPath, "backups", fileName);
                if (!System.IO.File.Exists(filePath)) return NotFound();

                string json = await System.IO.File.ReadAllTextAsync(filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<SnapshotData>(json);

                if (data == null) return BadRequest("Invalid snapshot file.");

                // --- CAUTION: DESTRUCTIVE OPERATION ---
                // Clear existing data in correct order (Children first)
                _context.Sales.RemoveRange(_context.Sales);
                _context.Expenses.RemoveRange(_context.Expenses);
                _context.GoalTargets.RemoveRange(_context.GoalTargets);
                _context.KpiThresholds.RemoveRange(_context.KpiThresholds);
                _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
                // Note: We don't wipe Users for safety (to avoid locking yourself out)
                _context.Branches.RemoveRange(_context.Branches);
                await _context.SaveChangesAsync();

                // Re-insert data preserving IDs for relationship integrity
                if (data.Branches != null) _context.Branches.AddRange(data.Branches);
                if (data.Sales != null) _context.Sales.AddRange(data.Sales);
                if (data.Expenses != null) _context.Expenses.AddRange(data.Expenses);
                if (data.GoalTargets != null) _context.GoalTargets.AddRange(data.GoalTargets);
                if (data.KpiThresholds != null) _context.KpiThresholds.AddRange(data.KpiThresholds);
                if (data.ActivityLogs != null) _context.ActivityLogs.AddRange(data.ActivityLogs);
                
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SYSTEM_RESTORE", $"Full system recovery completed from snapshot: {fileName}");

                TempData["Message"] = "System restored successfully! All data, relationships, and states have been recovered.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Restore failed: {ex.Message}";
            }
            return RedirectToAction(nameof(Archives));
        }

        private class SnapshotData
        {
            public List<Branch>? Branches { get; set; }
            public List<Sale>? Sales { get; set; }
            public List<Expense>? Expenses { get; set; }
            public List<GoalTarget>? GoalTargets { get; set; }
            public List<KpiThreshold>? KpiThresholds { get; set; }
            public List<ActivityLogEntry>? ActivityLogs { get; set; }
            public List<UserSnapshot>? Users { get; set; }
        }

        public class UserSnapshot
        {
            public string Id { get; set; } = string.Empty;
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public string? FullName { get; set; }
            public Guid? BranchId { get; set; }
            public string? Role { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UnarchiveBranch(Guid id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                branch.IsArchived = false;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_RESTORE", $"Restored branch: {branch.Name}");
                TempData["Message"] = $"Branch '{branch.Name}' has been restored.";
            }
            return RedirectToAction(nameof(Archives));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UnarchiveStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
                await _auditLog.LogActivityAsync("STAFF_RESTORE", $"Restored staff account: {user.FullName}");
                TempData["Message"] = $"Staff member '{user.FullName}' has been restored to active status.";
            }
            return RedirectToAction(nameof(Archives));
        }
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _context.SystemNotifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
            return Json(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(Guid id)
        {
            var notif = await _context.SystemNotifications.FindAsync(id);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
        [HttpGet]
        public async Task<IActionResult> SabotageSandawa()
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Name.Contains("Sandawa"));
            if (branch == null) return Content("Sandawa branch not found.");

            var expense = new Expense
            {
                Description = "EMERGENCY SYSTEM FAILURE (TEST)",
                Amount = 500000m,
                Category = "Other",
                Date = DateTime.Now,
                BranchId = branch.Id
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return Content("Sandawa Sabotaged! Margin is now deep in the red. Go test the Threshold Commit now!");
        }

        [HttpGet]
        public async Task<IActionResult> RenameSandawaManager()
        {
            var manager = await _userManager.FindByEmailAsync("manager@coretex.com") 
                        ?? await _userManager.FindByNameAsync("manager@coretex.com");
                        
            if (manager == null) return Content("Account 'manager@coretex.com' not found in database.");

            string newEmail = "manager.sandawa@coretex.com";
            
            // Update Identity Fields
            manager.Email = newEmail;
            manager.UserName = newEmail;
            manager.NormalizedEmail = newEmail.ToUpper();
            manager.NormalizedUserName = newEmail.ToUpper();

            var result = await _userManager.UpdateAsync(manager);
            if (result.Succeeded) return Content($"SUCCESS: {manager.Email} is now the official manager login.");
            return Content("Failed to update manager identity.");
        }

        [HttpGet]
        public async Task<IActionResult> ClearDemoData()
        {
            var submissions = await _context.BranchSubmissions.ToListAsync();
            _context.BranchSubmissions.RemoveRange(submissions);

            var sales = await _context.Sales.ToListAsync();
            _context.Sales.RemoveRange(sales);

            var expenses = await _context.Expenses.ToListAsync();
            _context.Expenses.RemoveRange(expenses);

            await _context.SaveChangesAsync();
            
            await _auditLog.LogActivityAsync("FULL_DATA_WIPE", "All Sales, Expenses, and Submissions were purged to restore a clean production state.");
            
            return Content("Success! ALL data (Sales, Expenses, Submissions) has been purged. Your dashboard will now be 100% empty until you record new transactions.");
        }

        [HttpGet]
        public async Task<IActionResult> SeedCharts()
        {
            var activeBranches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            if (activeBranches.Count == 0) return Content("No active branches found.");

            foreach (var branch in activeBranches)
            {
                // Clear old ones first to prevent duplicates
                var existing = await _context.BranchSubmissions
                    .Where(s => s.BranchId == branch.Id && (s.SubmissionMonth == 3 || s.SubmissionMonth == 4))
                    .ToListAsync();
                _context.BranchSubmissions.RemoveRange(existing);

                // Inject varied data for realism
                decimal revMarch = branch.Name.Contains("HQ") ? 850000 : 620000;
                decimal revApril = branch.Name.Contains("HQ") ? 920000 : 710000;
                decimal expMarch = branch.Name.Contains("HQ") ? 320000 : 280000;
                decimal expApril = branch.Name.Contains("HQ") ? 345000 : 310000;

                // MARCH
                _context.BranchSubmissions.Add(new BranchSubmission {
                    BranchId = branch.Id, SubmissionYear = 2026, SubmissionMonth = 3,
                    SalesRevenue = revMarch, Expenses = expMarch, Cogs = revMarch * 0.2m, Rent = 50000, Salaries = 100000, Utilities = 20000,
                    Status = "Submitted", SubmittedAt = DateTime.Now.AddMonths(-2)
                });

                // APRIL
                _context.BranchSubmissions.Add(new BranchSubmission {
                    BranchId = branch.Id, SubmissionYear = 2026, SubmissionMonth = 4,
                    SalesRevenue = revApril, Expenses = expApril, Cogs = revApril * 0.22m, Rent = 50000, Salaries = 105000, Utilities = 20000,
                    Status = "Submitted", SubmittedAt = DateTime.Now.AddMonths(-1)
                });
            }

            await _context.SaveChangesAsync();
            return Content($"Success! March and April data injected for {activeBranches.Count} branches. Refresh your CEO Dashboard to see the comparative data.");
        }

        private async Task CreateMasterAccount(string email, string role, string fullName, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    BranchId = null
                };
                await _userManager.CreateAsync(user, password);
            }

            // Ensure role is assigned
            if (!await _userManager.IsInRoleAsync(user, user.Role ?? role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            
            // Force password sync
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, password);
        }

        [HttpGet]
        public async Task<IActionResult> GlobalCorporateReset()
        {
            // 2. RE-CREATE EXECUTIVE & MASTER ADMIN (The Leaders)
            await CreateMasterAccount("admin@coretex.com", "ADMIN", "Master Systems Admin", "PasswordAdmin1234!");
            await CreateMasterAccount("ceo@coretex.com", "CEO", "Chief Executive Officer", "PasswordCeo1234!");

            var branchNames = new[] { "Sandawa", "Mintal", "Toril", "Matina", "Buhangin" };
            
            // 1. FORCE DETACH ALL FOREIGN KEYS (The Nuclear Option)
            await _context.Database.ExecuteSqlRawAsync("UPDATE AspNetUsers SET BranchId = NULL");
            await _context.Database.ExecuteSqlRawAsync("UPDATE DailySummaries SET BranchId = NULL");
            await _context.Database.ExecuteSqlRawAsync("UPDATE GeneratedReports SET BranchId = NULL");

            // 2. PURGE ALL STAFF (Except current CEO for safety)
            var currentUserId = _userManager.GetUserId(User);
            var allUsers = await _userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                if (user.Id == currentUserId || user.Email == "ceo@coretex.com" || user.Email == "admin@coretex.com") continue;
                await _userManager.DeleteAsync(user);
            }

            // 3. HARD DELETE DECOMMISSIONED BRANCHES
            var allBranches = await _context.Branches.ToListAsync();
            foreach (var b in allBranches) {
                if (!branchNames.Contains(b.Name))
                {
                    _context.Branches.Remove(b);
                }
                else
                {
                    b.IsArchived = true; 
                    b.IsActive = false;
                    _context.Branches.Update(b);
                }
            }
            await _context.SaveChangesAsync();

            // 3. SETUP 5 CORE BRANCHES (Exact Names)
            var activeBranches = new List<Branch>();
            foreach (var name in branchNames)
            {
                string code = "CORETEX-" + name.ToUpper();
                // Match by exact name OR branch code to prevent duplicates
                var b = allBranches.FirstOrDefault(x => x.Name.Trim() == name || x.BranchCode == code);
                
                if (b == null)
                {
                    b = new Branch { 
                        Id = Guid.NewGuid(),
                        Name = name, 
                        BranchCode = code, 
                        Address = name + ", Davao City",
                        IsActive = true,
                        IsArchived = false
                    };
                    _context.Branches.Add(b);
                }
                else
                {
                    b.Name = name.Trim(); // Force exact name (remove "Branch", "Hub", etc.)
                    b.BranchCode = code;
                    b.IsArchived = false;
                    b.IsActive = true;
                    _context.Branches.Update(b);
                }
                activeBranches.Add(b);
            }
            await _context.SaveChangesAsync();

            // 4. DEPLOY FRESH WORKFORCE
            int userCount = 0;
            foreach (var branch in activeBranches)
            {
                string bName = branch.Name;
                
                // Roles to deploy per branch (1 Manager, 1 Finance, 5 Cashiers)
                var pList = new List<dynamic> {
                    new { Role = "BRANCH_ADMIN", Index = 1, Prefix = "manager", RoleName = "Manager" },
                    new { Role = "FINANCE", Index = 1, Prefix = "finance", RoleName = "Finance" },
                    new { Role = "CASHIER", Index = 1, Prefix = "cashier1", RoleName = "Cashier" },
                    new { Role = "CASHIER", Index = 2, Prefix = "cashier2", RoleName = "Cashier" },
                    new { Role = "CASHIER", Index = 3, Prefix = "cashier3", RoleName = "Cashier" },
                    new { Role = "CASHIER", Index = 4, Prefix = "cashier4", RoleName = "Cashier" },
                    new { Role = "CASHIER", Index = 5, Prefix = "cashier5", RoleName = "Cashier" }
                };

                foreach (var p in pList)
                {
                    string email = $"{p.Prefix}.{bName.ToLower()}@coretex.com";
                    // Password Pattern: [RoleName][BranchName][Index]2026!@#
                    string password = p.RoleName == "Cashier" 
                        ? $"{p.RoleName}{bName}{p.Index}2026!@#" 
                        : $"{p.RoleName}{bName}2026!@#";
                    
                    var user = new AppUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = $"{bName} {p.RoleName} {p.Index}",
                        BranchId = branch.Id,
                        Role = p.Role,
                        IsActive = true,
                        EmailConfirmed = true
                    };
                    
                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded) userCount++;
                }
            }

            await _auditLog.LogActivityAsync("GLOBAL_RESET", $"Corporate migration complete. Purged old data. 5 nodes active, {userCount} personnel deployed.");
            return Content($"CORPORATE MIGRATION SUCCESS: Purged all old staff. 5 Branches ({string.Join(", ", branchNames)}) synchronized. {userCount} staff accounts deployed with exact patterns.");
        }

        [HttpPost]
        public async Task<IActionResult> SendReminder(string branchId, string message)
        {
            if (!Guid.TryParse(branchId, out Guid branchGuid))
                return BadRequest("Invalid Node ID Format");

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchGuid);
            if (branch == null) return BadRequest("Invalid Node ID");

            // 1. Log the Audit Event
            await _auditLog.LogActivityAsync(
                "SECURITY_ALERT",
                $"Compliance reminder dispatched to {branch.Name} Node."
            );

            // 2. Find the Finance Officer or Manager for this branch
            var recipients = await _userManager.Users
                .Where(u => u.BranchId == branchGuid)
                .ToListAsync();

            var emails = recipients.Select(u => u.Email).ToList();

            // 3. Send Email via IEmailSender (SendGrid/SMTP)
            foreach (var email in emails)
            {
                if (!string.IsNullOrEmpty(email))
                {
                    await _emailSender.SendEmailAsync(
                        email, 
                        $"[URGENT] Compliance Alert: {branch.Name} Node", 
                        $"<div style='font-family:sans-serif; padding:30px; border:2px solid #f59e0b; border-radius:20px; background-color:#fffaf0;'>" +
                        $"<div style='text-align:center; margin-bottom:20px;'>" +
                        $"<span style='background-color:#f59e0b; color:white; padding:5px 15px; border-radius:full; font-size:12px; font-weight:bold; text-transform:uppercase;'>System Directive</span>" +
                        $"</div>" +
                        $"<h2 style='color:#92400e; margin-top:0; text-align:center;'>⚠️ Compliance Reminder</h2>" +
                        $"<div style='background-color:white; padding:20px; border-radius:15px; border:1px solid #fed7aa; margin:20px 0;'>" +
                        $"<p style='margin:0; color:#451a03; font-weight:bold; font-size:16px; text-align:center;'>BRANCH NODE: <span style='color:#f59e0b; text-decoration:underline;'>{branch.Name.ToUpper()}</span></p>" +
                        $"</div>" +
                        $"<p style='color:#78350f; line-height:1.6;'>{message}</p>" +
                        $"<hr style='border:0; border-top:1px solid #fed7aa; margin:20px 0;' />" +
                        $"<p style='font-size:11px; color:#9a3412; text-align:center; font-style:italic;'>This is an official tactical directive from the Coretex Executive Intelligence System.</p>" +
                        $"</div>"
                    );
                }
            }

            // 4. Also add a system notification
            foreach (var user in recipients)
            {
                await _notificationService.CreateNotificationAsync(
                    branchGuid,
                    "URGENT: Submission Required",
                    $"The System Admin has requested an immediate financial submission for {branch.Name}.",
                    "alert"
                );
            }

            return Ok();
        }
    }
}
