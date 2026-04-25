using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CEO")]
    public class CeoController : Controller
    {
        private readonly Services.AnalyticsService _analytics;

        public CeoController(Services.AnalyticsService analytics)
        {
            _analytics = analytics;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard(Guid? branchId)
        {
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
            var data = await _analytics.GetExpenseCategoriesAsync();
            return View(data);
        }

        public async Task<IActionResult> AnalyticsForecast()
        {
            ViewBag.Forecast = await _analytics.GetSalesForecastAsync(null);
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
            ViewBag.Forecast = await _analytics.GetSalesForecastAsync(null);
            return View();
        }

        public IActionResult News()
        {
            return View();
        }

        public IActionResult ReportsGenerate()
        {
            return RedirectToAction("ReportSchedule", "Admin");
        }

        public IActionResult Reports()
        {
            return RedirectToAction("BranchSubmissions", "Admin");
        }
    }
}
