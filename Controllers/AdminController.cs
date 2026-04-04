using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult UserManagement()
        {
            return View();
        }

        public IActionResult SystemSetup()
        {
            return View();
        }

        public IActionResult ActivityLog()
        {
            return View();
        }
    }
}
