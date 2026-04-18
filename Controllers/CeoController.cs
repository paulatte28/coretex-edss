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

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.MonthlyData = await _analytics.GetMonthlyProfitLossAsync();
            ViewBag.BranchData = await _analytics.GetBranchPerformanceAsync();
            ViewBag.ExpenseData = await _analytics.GetExpenseCategoriesAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyAnalytics() => Json(await _analytics.GetMonthlyProfitLossAsync());

        [HttpGet]
        public async Task<IActionResult> GetBranchAnalytics() => Json(await _analytics.GetBranchPerformanceAsync());

        [HttpGet]
        public async Task<IActionResult> GetExpenseAnalytics() => Json(await _analytics.GetExpenseCategoriesAsync());

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
