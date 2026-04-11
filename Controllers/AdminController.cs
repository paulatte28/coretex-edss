using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    public class AdminController : Controller
    {
        // Admin Dashboard / Overview
        public IActionResult Index()
        {
            return View();
        }

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
