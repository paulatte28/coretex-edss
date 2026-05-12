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
            var sendGridKey = _config["SendGrid:ApiKey"];
            var sendGridFrom = _config["SendGrid:FromEmail"];
            var smtpFrom = _config["Email:From"] ?? _config["Email:User"];
            
            // --- TEST REDIRECT LOGIC (Applies globally for demo) ---
            // Reroute @coretex.com emails to the developer's real email for testing
            var realRedirectEmail = _config["Email:User"] ?? _config["SendGrid:FromEmail"];

            if (email.EndsWith("@coretex.com") && !string.IsNullOrEmpty(realRedirectEmail))
            {
                _logger.LogWarning("TEST REDIRECT: Intercepted email for {FakeEmail}. Rerouting to {RealEmail} for demo.", email, realRedirectEmail);
                email = realRedirectEmail; 
            }

            // --- SENDGRID PATH (Primary) ---
            if (!string.IsNullOrEmpty(sendGridKey) && !sendGridKey.Contains("YOUR_"))
            {
                try
                {
                    var client = new SendGrid.SendGridClient(sendGridKey);
                    var from = new SendGrid.Helpers.Mail.EmailAddress(sendGridFrom ?? smtpFrom ?? "security@coretex.com", "Coretex Executive Security");
                    var to = new SendGrid.Helpers.Mail.EmailAddress(email);
                    var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
                    var response = await client.SendEmailAsync(msg);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Real-time email sent successfully to {Email} via SendGrid", email);
                        return;
                    }
                    _logger.LogWarning("SendGrid failed with status {Status}. Falling back to SMTP.", response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendGrid Exception. Falling back to SMTP.");
                }
            }

            // --- SMTP FALLBACK PATH ---
            var smtpHost = _config["Email:Host"];
            var smtpPort = _config.GetValue<int>("Email:Port", 465);
            var smtpUser = _config["Email:User"];
            var smtpPass = _config["Email:Password"];

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
                    "CONTENT: {Message}\n" +
                    "==============================================", 
                    email, subject, htmlMessage);
                return;
            }

            try 
            {
                var message = new MimeMessage();
                // IMPORTANT: For Gmail SMTP, the 'From' address must match the authenticated account
                message.From.Add(new MailboxAddress("Coretex Security", smtpFrom ?? smtpUser));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

                using var client = new SmtpClient();
                var secureSocketOptions = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
                await client.AuthenticateAsync(smtpUser ?? "", smtpPass ?? "");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email} via MailKit Fallback", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailKit failed to send email to {Email}", email);
                throw;
            }
        }
    }
}
