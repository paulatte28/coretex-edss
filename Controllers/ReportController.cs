using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coretex_finalproj.Services;

namespace coretex_finalproj.Controllers
{
    public class ReportController : Controller
    {
        private readonly NotificationService _notificationService;

        public ReportController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateExecutiveReport(string emailAddress)
        {
            // Simulate generation of a PDF or HTML report based on business data
            string reportContent = "<h1>Executive Report</h1><p>Your KPI metrics and predictive trends are stable.</p>";
            
            bool success = await _notificationService.SendExecutiveReportEmailAsync(emailAddress, "Monthly Executive Summary", reportContent);
            
            if (success) {
                TempData["Message"] = $"Executive report generated and emailed to {emailAddress}.";
            } else {
                TempData["Error"] = "Failed to send the report.";
            }

            return RedirectToAction("ExecutiveReporting", "Dashboard");
        }
        
        [HttpPost]
        public async Task<IActionResult> TriggerSmsAlert(string phoneNumber)
        {
            // Manual trigger to simulate an SMS alert when threshold falls
            bool success = await _notificationService.SendSmsAlertAsync(phoneNumber, "ALERT: Sales Revenue has dropped below the target threshold.");
            
            if (success) {
                TempData["Message"] = $"SMS Alert sent to {phoneNumber}.";
            } else {
                TempData["Error"] = "Failed to send the SMS alert.";
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
