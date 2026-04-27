using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MimeKit.Text;

namespace coretex_finalproj.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _config;

        public EmailSender(ILogger<EmailSender> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpHost = _config["Email:Host"];
            var smtpPort = _config.GetValue<int>("Email:Port", 465);
            var smtpUser = _config["Email:User"];
            var smtpPass = _config["Email:Password"];
            var fromEmail = _config["Email:From"] ?? "security@coretex.com";

            // Check for placeholders to allow Mock Mode
            if (string.IsNullOrEmpty(smtpHost) || 
                string.IsNullOrEmpty(smtpUser) || 
                smtpUser.Contains("[") || 
                smtpUser.Contains("REQUIRED") ||
                smtpHost.Contains("coretex.com"))
            {
                _logger.LogWarning("EMAIL MOCK MODE: Placeholder detected. Printing to console.");
                _logger.LogInformation("\n" +
                    "==============================================\n" +
                    "TO: {Email}\n" +
                    "SUBJECT: {Subject}\n" +
                    "OTP CODE: {Message}\n" +
                    "==============================================", 
                    email, subject, htmlMessage);
                return;
            }

            try 
            {
                // --- DEVELOPER TESTING OVERRIDE ---
                // If the recipient is a "fake" system account, redirect it to your real Gmail
                if (email.EndsWith("@coretex.com") && !string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning("TEST REDIRECT: Intercepted email for {FakeEmail}. Rerouting to {RealEmail} for demo.", email, smtpUser);
                    email = smtpUser; // Reroute to your real inbox
                }

                // Force use of modern TLS protocols for security
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Coretex Security", fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

                using var client = new SmtpClient();
                
                // For Port 465, we use SslOnConnect
                // For Port 587, we would use StartTls
                var secureSocketOptions = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Real-time email sent successfully to {Email} via MailKit", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailKit failed to send email to {Email}", email);
                throw;
            }
        }
    }
}
