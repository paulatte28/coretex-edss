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

        public IActionResult KpiProfitMargin()
        {
            return View();
        }

        public IActionResult KpiExpenseRatio()
        {
            return View();
        }

        public IActionResult AnalyticsForecast()
        {
            return View();
        }

        public IActionResult AnalyticsExpenseTrend()
        {
            return View();
        }

        public IActionResult KpiRiskScore()
        {
            return View();
        }

        public IActionResult AnalyticsHealthSummary()
        {
            return View();
        }

        public IActionResult BranchesCompare()
        {
            return View();
        }

        public IActionResult AnalyticsMom()
        {
            return View();
        }

        public IActionResult Charts()
        {
            return View();
        }

        public IActionResult AnalyticsPredictive()
        {
            return View();
        }

        public IActionResult News()
        {
            return View();
        }

        public IActionResult ReportsGenerate()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
