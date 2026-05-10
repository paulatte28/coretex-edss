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
    [Authorize(Roles = "ADMIN,BRANCH_ADMIN,CEO,FINANCE")]
    public class AdminController(
        AnalyticsService analytics,
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        AuditLoggingService auditLog,
        IEmailSender emailSender) : Controller
    {
        private readonly AnalyticsService _analytics = analytics;
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly AuditLoggingService _auditLog = auditLog;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender = emailSender;

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

                // Check for any active branches that haven't submitted this month
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var activeBranchCount = await _context.Branches.CountAsync(b => b.IsActive && !b.IsArchived);
                var submittedCount = await _context.BranchSubmissions
                    .Where(s => s.SubmittedAt.Month == currentMonth && s.SubmittedAt.Year == currentYear)
                    .Select(s => s.BranchId)
                    .Distinct()
                    .CountAsync();

                ViewBag.PendingSubmissions = activeBranchCount - submittedCount;
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
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
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
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("BRANCH_ARCHIVE", $"Archived branch: {branch.Name}");
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

            var oldRole = user.Role;
            var oldStatus = user.IsActive;

            user.FullName = fullName;
            user.Role = role.ToUpper();
            user.IsActive = isActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // LOGGING: Check if this was a sensitive change
                if (oldRole != user.Role)
                {
                    await _auditLog.LogActivityAsync("SECURITY_PROMOTION", $"CRITICAL: {user.Email} promoted/reassigned from {oldRole} to {user.Role} by Manager.");
                }
                
                if (oldStatus != user.IsActive)
                {
                    await _auditLog.LogActivityAsync("USER_STATUS_CHANGE", $"User {user.Email} access {(user.IsActive ? "Enabled" : "Disabled")} by Manager.");
                }

                await _auditLog.LogActivityAsync("USER_UPDATE", $"Personnel record updated: {user.Email}");
                TempData["Message"] = "Personnel records updated successfully.";
            }
            else
            {
                TempData["Error"] = "Update failed: " + string.Join(", ", result.Errors.Select(e => e.Description));
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

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _auditLog.LogActivityAsync("USER_TERMINATE", $"Terminated access for {user.Email}.");
                TempData["Message"] = "Personnel access revoked successfully.";
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
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (result.Succeeded)
                {
                    await _auditLog.LogActivityAsync("USER_PASSWORD_RESET", $"Successfully reset security credentials for: {user.Email}");
                    TempData["Message"] = $"Password for {user.Email} has been reset successfully.";
                }
                else
                {
                    TempData["Error"] = "PASSWORD REJECTED: " + string.Join(" ", result.Errors.Select(e => e.Description));
                }
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                await _auditLog.LogActivityAsync("USER_STATUS_TOGGLE", $"Admin manually {(user.IsActive ? "Activated" : "Deactivated")} account: {user.Email}");
                TempData["Message"] = $"User {user.Email} is now {(user.IsActive ? "ACTIVE" : "INACTIVE")}.";
            }
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
            var currentUser = await _userManager.GetUserAsync(User);
            IQueryable<ActivityLogEntry> query = _context.ActivityLogs.Include(l => l.Branch);

            if (currentUser?.BranchId != null && !User.IsInRole("CEO") && !User.IsInRole("ADMIN"))
            {
                query = query.Where(l => l.BranchId == currentUser.BranchId);
            }

            // DEPARTMENT ISOLATION: Finance only sees Finance-related logs
            if (User.IsInRole("FINANCE"))
            {
                query = query.Where(l => l.ActionType.Contains("EXPENSE") || l.ActionType.Contains("MONTH") || l.ActionType.Contains("FINANCE"));
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
        public async Task<IActionResult> SaveKpiThreshold([FromBody] KpiThreshold threshold)
        {
            if (threshold == null) return BadRequest();

            var existing = await _context.KpiThresholds
                .FirstOrDefaultAsync(t => t.BranchId == threshold.BranchId && t.IsActive);

            if (existing != null)
            {
                existing.MinProfitMargin = threshold.MinProfitMargin;
                existing.MaxExpenseRatio = threshold.MaxExpenseRatio;
                existing.MinMonthlyProfit = threshold.MinMonthlyProfit;
                existing.RiskAlertLevel = threshold.RiskAlertLevel;
                _context.KpiThresholds.Update(existing);
            }
            else
            {
                _context.KpiThresholds.Add(threshold);
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("KPI_CONFIG", "Updated strategic KPI safety thresholds for the branch.", threshold.BranchId);

            // --- DIRECT RISK DETECTION ENGINE ---
            var snapshot = await _analytics.GetDashboardSnapshotAsync(threshold.BranchId);
            bool alertTriggered = false;
            
            if (snapshot.ProfitMargin < threshold.MinProfitMargin)
            {
                var alert = new SystemNotification
                {
                    Title = "Strategic Breach Detected",
                    Message = $"Branch operational health ({snapshot.ProfitMargin:F1}%) is below your new executive target ({threshold.MinProfitMargin:F1}%).",
                    Type = "KPI",
                    Severity = "red",
                    BranchId = threshold.BranchId,
                    CreatedAt = DateTime.Now
                };
                _context.SystemNotifications.Add(alert);
                await _context.SaveChangesAsync();
                alertTriggered = true;
            }

            return Json(new { success = true, alerted = alertTriggered });
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
            var model = new ArchivesViewModel
            {
                ArchivedBranches = await _context.Branches.Where(b => b.IsArchived).ToListAsync(),
                ArchivedSales = await _context.Sales.Include(s => s.Branch).Where(s => s.IsArchived).ToListAsync(),
                ArchivedExpenses = await _context.Expenses.Include(e => e.Branch).Where(e => e.IsArchived).ToListAsync()
            };
            return View(model);
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
        public async Task<IActionResult> UnarchiveSale(Guid id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null)
            {
                sale.IsArchived = false;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SALE_RESTORE", $"Restored transaction: #{sale.OrderId}");
                TempData["Message"] = $"Transaction #{sale.OrderId} has been restored.";
            }
            return RedirectToAction(nameof(Archives));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UnarchiveExpense(Guid id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                expense.IsArchived = false;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("EXPENSE_RESTORE", $"Restored expense: {expense.Description}");
                TempData["Message"] = $"Expense '{expense.Description}' has been restored.";
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
    }
}

