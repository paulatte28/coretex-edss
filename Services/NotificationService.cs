using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace coretex_finalproj.Services
{
    public class NotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationService> _logger;
        private readonly AuditLoggingService _auditLog;
        private readonly Data.ApplicationDbContext _context;

        public NotificationService(IConfiguration config, ILogger<NotificationService> logger, AuditLoggingService auditLog, Data.ApplicationDbContext context)
        {
            _config = config;
            _logger = logger;
            _auditLog = auditLog;
            _context = context;
        }

        public async Task CreateNotificationAsync(Guid branchId, string title, string message, string type)
        {
            var notification = new Models.SystemNotification
            {
                Title = title,
                Message = message,
                Type = type.ToUpper(),
                Severity = type.ToLower() == "alert" ? "yellow" : "blue",
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.SystemNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SendExecutiveAlertEmailAsync(string toEmail, string alertTitle, string detail, string severity = "red", Guid? branchId = null)
        {
            // Persist to Database
            var notification = new Models.SystemNotification
            {
                Title = alertTitle,
                Message = detail,
                Type = "KPI_BREACH",
                Severity = severity,
                BranchId = branchId,
                ActionUrl = severity == "red" ? "/ceo/kpi/risk-score" : "/ceo/kpi/profit-margin"
            };
            _context.SystemNotifications.Add(notification);
            await _context.SaveChangesAsync();

            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"] ?? "alerts@coretex.com";

            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("stub"))
            {
                _logger.LogWarning("SendGrid API Key not configured. Logging to Audit Log instead.");
                await _auditLog.LogActivityAsync("ALERT_SIMULATION", $"[SIMULATED] Email Alert: {alertTitle} - {detail}");
                return true;
            }

            return await SendEmailInternalAsync(toEmail, $"[CORETEX CRITICAL] {alertTitle}", $"<h3>Critical System Alert</h3><p>{detail}</p>", "ALERT_SENT", "ALERT_FAILURE");
        }

        public async Task<bool> SendExecutiveReportEmailAsync(string toEmail, string subject, string content)
        {
            return await SendEmailInternalAsync(toEmail, subject, content, "REPORT_EMAIL_SENT", "REPORT_EMAIL_FAILURE");
        }

        private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string content, string successType, string failureType)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"] ?? "reports@coretex.com";

            try
            {
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, "Coretex Executive System");
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, content);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SendGrid Email sent to {toEmail}");
                    await _auditLog.LogActivityAsync(successType, $"Email successfully dispatched to {toEmail}");
                    return true;
                }
                
                _logger.LogWarning($"SendGrid Email failed with status: {response.StatusCode}");
                await _auditLog.LogActivityAsync(failureType, $"SendGrid API returned status {response.StatusCode}");
                return false;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SendGrid email to {toEmail}");
                await _auditLog.LogActivityAsync(failureType, $"Email Exception: {ex.Message}");
                return false;
            }
        }
    }
}
