using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly Services.AnalyticsService _analytics;

        public AdminController(Services.AnalyticsService analytics)
        {
            _analytics = analytics;
        }

        // Admin Dashboard / Overview
        public async Task<IActionResult> Index()
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

        // Branch Management (Davao, Tagum, Digos, etc.)
        public IActionResult BranchManagement()
        {
            return View();
        }

        // User Management (CEO, Finance Officers)
        public IActionResult UserManagement()
        {
            return View();
        }

        // KPI Threshold Configuration
        public IActionResult KPIThresholds()
        {
            return View();
        }

        // Goals & Targets Setting
        public IActionResult GoalsTargets()
        {
            return View();
        }

        // Report Schedule Configuration
        public IActionResult ReportSchedule()
        {
            return View();
        }

        // Activity & Audit Log
        public IActionResult ActivityLog()
        {
            return View();
        }

        // Branch Submissions Monitoring
        public IActionResult BranchSubmissions()
        {
            return View();
        }

        // Audit Trail
        public IActionResult AuditTrail()
        {
            return View();
        }
    }
}
