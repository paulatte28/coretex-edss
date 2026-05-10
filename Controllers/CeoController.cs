using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using coretex_finalproj.Data;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CEO")]
    public class CeoController(
        Data.ApplicationDbContext context,
        AnalyticsService analytics,
        NewsService news,
        TrendService trends,
        UserManager<AppUser> userManager,
        AuditLoggingService auditLog) : Controller
    {
        private readonly Data.ApplicationDbContext _context = context;
        private readonly AnalyticsService _analytics = analytics;
        private readonly NewsService _news = news;
        private readonly TrendService _trends = trends;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly AuditLoggingService _auditLog = auditLog;

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard(Guid? branchId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserEmail = user?.Email ?? "";
            
            ViewBag.SelectedBranchId = branchId;
            ViewBag.Branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync(branchId);
            ViewBag.BranchData = await _analytics.GetBranchPerformanceAsync();
            ViewBag.ExpenseData = await _analytics.GetExpenseCategoriesAsync(branchId);
            ViewBag.Snapshot = await _analytics.GetDashboardSnapshotAsync(branchId);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyAnalytics(Guid? branchId) => Json(await _analytics.GetMonthlyProfitLossAsync(branchId));

        [HttpGet]
        public async Task<IActionResult> GetBranchAnalytics() => Json(await _analytics.GetBranchPerformanceAsync());

        [HttpGet]
        public async Task<IActionResult> GetExpenseAnalytics(Guid? branchId) => Json(await _analytics.GetExpenseCategoriesAsync(branchId));

        [HttpGet]
        public async Task<IActionResult> GetLiveNews(string category)
        {
            var newsJson = await _news.GetLiveNewsAsync(category);
            if (string.IsNullOrEmpty(newsJson)) return BadRequest();
            return Content(newsJson, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetMarketTrends(string query)
        {
            var trendsJson = await _trends.GetMarketTrendsAsync(query);
            if (string.IsNullOrEmpty(trendsJson)) return BadRequest();
            return Content(trendsJson, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _context.SystemNotifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
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

        public async Task<IActionResult> KpiProfitMargin()
        {
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.BranchData = await _analytics.GetBranchPerformanceAsync();
            
            // Get HQ threshold or first active
            var threshold = await _context.KpiThresholds.FirstOrDefaultAsync(t => t.IsActive);
            ViewBag.Threshold = threshold?.MinProfitMargin ?? 15m;

            return View();
        }

        public async Task<IActionResult> KpiExpenseRatio()
        {
            ViewBag.Snapshot = await _analytics.GetDashboardSnapshotAsync();
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.ExpenseData = await _analytics.GetExpenseCategoriesAsync();
            return View();
        }

        public async Task<IActionResult> AnalyticsForecast()
        {
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.ForecastAmount = await _analytics.GetSalesForecastAsync(null);
            return View();
        }

        public async Task<IActionResult> AnalyticsExpenseTrend()
        {
            var data = await _analytics.GetExpenseCategoriesAsync();
            return View(data);
        }

        public async Task<IActionResult> KpiRiskScore()
        {
            var snapshot = await _analytics.GetDashboardSnapshotAsync();
            return View(snapshot);
        }

        public async Task<IActionResult> AnalyticsHealthSummary()
        {
            var snapshot = await _analytics.GetDashboardSnapshotAsync();
            return View(snapshot);
        }

        public async Task<IActionResult> BranchesCompare()
        {
            var data = await _analytics.GetBranchPerformanceAsync();
            return View(data);
        }

        public async Task<IActionResult> AnalyticsMom()
        {
            var data = await _analytics.GetMonthlyProfitLossAsync();
            return View(data);
        }

        public async Task<IActionResult> Charts()
        {
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.ExpenseData = await _analytics.GetExpenseCategoriesAsync();
            return View();
        }

        public async Task<IActionResult> AnalyticsPredictive()
        {
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.ForecastAmount = await _analytics.GetSalesForecastAsync(null);
            return View();
        }

        public IActionResult News()
        {
            return View();
        }

        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> Reports()
        {
            var submissions = await _context.BranchSubmissions
                .Include(s => s.Branch)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.GeneratedReports = await _context.GeneratedReports
                .OrderByDescending(r => r.GeneratedAt)
                .Take(20)
                .ToListAsync();

            return View(submissions);
        }

        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> ReportsGenerate()
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
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsSeries()
        {
            // Fetch all confirmed submissions first (Confirmed by FO)
            var submissions = await _context.BranchSubmissions
                .Include(s => s.Branch)
                .OrderBy(s => s.SubmissionYear)
                .ThenBy(s => s.SubmissionMonth)
                .ToListAsync();

            var series = submissions.GroupBy(s => new { s.SubmissionYear, s.SubmissionMonth })
                .Select(g => new
                {
                    monthKey = $"{g.Key.SubmissionYear}-{g.Key.SubmissionMonth:D2}",
                    month = new DateTime(g.Key.SubmissionYear, g.Key.SubmissionMonth, 1).ToString("MMM yyyy"),
                    revenue = g.Sum(x => x.SalesRevenue),
                    expenses = g.Sum(x => x.Expenses),
                    netProfit = g.Sum(x => x.SalesRevenue - x.Expenses)
                })
                .ToList();

            // If no submissions yet, try to pull raw data from Sales/Expenses tables for current and past month
            if (series.Count == 0)
            {
                var now = DateTime.Now;
                for (int i = 5; i >= 0; i--)
                {
                    var d = now.AddMonths(-i);
                    var start = new DateTime(d.Year, d.Month, 1);
                    var end = start.AddMonths(1);

                    var rev = await _context.Sales.Where(s => s.Date >= start && s.Date < end && !s.IsArchived).SumAsync(s => s.Amount);
                    var exp = await _context.Expenses.Where(e => e.Date >= start && e.Date < end && !e.IsArchived).SumAsync(e => e.Amount);

                    series.Add(new {
                        monthKey = $"{d.Year}-{d.Month:D2}",
                        month = d.ToString("MMM yyyy"),
                        revenue = rev,
                        expenses = exp,
                        netProfit = rev - exp
                    });
                }
            }

            return Json(series);
        }

        [HttpGet]
        public async Task<IActionResult> GetBranchPerformance()
        {
            var performance = await _analytics.GetBranchPerformanceAsync();
            return Json(performance);
        }
        [HttpPost]
        public async Task<IActionResult> SaveReport([FromBody] GeneratedReport report)
        {
            if (report == null) return BadRequest();

            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName)) return Unauthorized();

            var user = await _userManager.FindByNameAsync(userName);
            
            report.GeneratedById = user?.Id;
            report.GeneratedAt = DateTime.Now;

            _context.GeneratedReports.Add(report);
            
            // Trigger a Global System Notification
            var notification = new SystemNotification
            {
                Title = "Strategic Directive Applied",
                Message = $"CEO has officially activated the '{report.Title}' strategy.",
                Type = "STRATEGY_ACTIVE",
                Severity = "indigo"
            };
            _context.SystemNotifications.Add(notification);
            
            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("REPORT_GENERATE", $"CEO generated a new executive summary: {report.Title}");

            return Json(new { success = true, id = report.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.GeneratedReports
                .Include(r => r.Branch)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new {
                    id = r.Id,
                    title = r.Title,
                    periodLabel = r.PeriodLabel,
                    generatedOn = r.GeneratedAt,
                    branchName = r.Branch != null ? r.Branch.Name : "All Branches"
                })
                .ToListAsync();

            return Json(reports);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.GeneratedReports.FindAsync(id);
            if (report == null) return NotFound();

            var reportTitle = report.Title;
            _context.GeneratedReports.Remove(report);
            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("REPORT_DELETE", $"CEO purged an executive report from the archives: {reportTitle}");
            return Json(new { success = true });
        }

        public async Task<IActionResult> AuditTrail()
        {
            var logs = await _context.ActivityLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }
        [HttpPost]
        public async Task<IActionResult> AutonomousPriceAdjustment([FromBody] AdjustmentRequest request)
        {
            var products = await _context.Products
                .Where(p => p.Category == request.Category)
                .ToListAsync();

            int count = 0;
            foreach (var product in products)
            {
                product.Price *= (1 + request.Percentage / 100);
                count++;
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("AUTO_PRICE_ADJUST", $"Autonomous {request.Percentage}% markup applied to {count} {request.Category} products.");

            return Json(new { success = true, count });
        }

        [HttpPost]
        public async Task<IActionResult> AutonomousStockRebalance([FromBody] RebalanceRequest request)
        {
            // 1. Identify the source branch (Sandawa as per UI)
            var sourceBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Name.Contains(request.SourceBranch));
            if (sourceBranch == null) return BadRequest(new { message = "Source branch not found" });

            // 2. Find target branch (any other active branch)
            var targetBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Id != sourceBranch.Id && !b.IsArchived);
            if (targetBranch == null) return BadRequest(new { message = "No target branch available for rebalancing" });

            // 3. Find products in the category with 'excess' stock (> 20)
            var products = await _context.Products
                .Where(p => p.BranchId == sourceBranch.Id && p.Category == request.Category && p.StockQuantity > 20)
                .ToListAsync();

            int transferCount = 0;
            int totalItems = 0;

            foreach (var p in products)
            {
                int moveAmount = p.StockQuantity / 2; // Move half the stock
                p.StockQuantity -= moveAmount;
                
                // Find or create product in target branch
                var targetProduct = await _context.Products.FirstOrDefaultAsync(tp => tp.BranchId == targetBranch.Id && tp.Name == p.Name);
                if (targetProduct != null)
                {
                    targetProduct.StockQuantity += moveAmount;
                }
                else
                {
                    // Create it if it doesn't exist
                    _context.Products.Add(new Product
                    {
                        Name = p.Name,
                        Category = p.Category,
                        Price = p.Price,
                        StockQuantity = moveAmount,
                        BranchId = targetBranch.Id
                    });
                }
                transferCount++;
                totalItems += moveAmount;
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("STOCK_REBALANCE", $"Autonomous rebalancing complete: {totalItems} items ({transferCount} SKUs) moved from {sourceBranch.Name} to {targetBranch.Name}.");

            return Json(new { success = true, skuCount = transferCount, totalItems, targetBranch = targetBranch.Name });
        }

        public class RebalanceRequest
        {
            public string Category { get; set; } = string.Empty;
            public string SourceBranch { get; set; } = string.Empty;
        }

        public class AdjustmentRequest
        {
            public string Category { get; set; } = string.Empty;
            public decimal Percentage { get; set; }
        }
    }
}
