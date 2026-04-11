using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    public class CeoController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            return View();
        }

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
