using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class NotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendSmsAlertAsync(string toPhoneNumber, string message)
        {
            var accountSid = _config["Twilio:AccountSid"];
            var authToken = _config["Twilio:AuthToken"];
            var fromNumber = _config["Twilio:FromNumber"];

            // SIMULATED implementation of Twilio
            // Would normally initialize TwilioClient: TwilioClient.Init(accountSid, authToken);
            // var msg = await MessageResource.CreateAsync(body: message, from: new Twilio.Types.PhoneNumber(fromNumber), to: new Twilio.Types.PhoneNumber(toPhoneNumber));
            
            _logger.LogInformation($"[MOCK TWILIO SMS] Sending to {toPhoneNumber}: {message}");
            await Task.Delay(100); // Simulate network call
            
            return true;
        }

        public async Task<bool> SendExecutiveReportEmailAsync(string toEmail, string subject, string content)
        {
            var apiKey = _config["SendGrid:ApiKey"];

            // SIMULATED implementation of SendGrid
            // var client = new SendGridClient(apiKey);
            // var from = new EmailAddress("reports@coretex.com", "Coretex System");
            // var to = new EmailAddress(toEmail);
            // var msg = MailHelper.CreateSingleEmail(from, to, subject, "Plain text", content);
            // var response = await client.SendEmailAsync(msg);

            _logger.LogInformation($"[MOCK SENDGRID EMAIL] Sending Email to {toEmail} with Subject: {subject}");
            await Task.Delay(100);

            return true;
        }
    }
}
