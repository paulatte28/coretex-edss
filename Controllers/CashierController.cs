using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CASHIER")]
    public class CashierController : Controller
    {
        public IActionResult Pos()
        {
            return View();
        }

        public IActionResult DailySummary()
        {
            return View();
        }

        public IActionResult Transactions()
        {
            return View();
        }
    }
}