using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "FINANCE")]
    public class FinanceController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult ExpensesCogs()
        {
            return View();
        }

        public IActionResult ExpensesRent()
        {
            return View();
        }

        public IActionResult ExpensesSalaries()
        {
            return View();
        }

        public IActionResult ExpensesUtilities()
        {
            return View();
        }

        public IActionResult Review()
        {
            return View();
        }

        public IActionResult Submit()
        {
            return View();
        }

        public IActionResult EditSubmission()
        {
            return View();
        }

        public IActionResult Submissions()
        {
            return View();
        }
    }
}
