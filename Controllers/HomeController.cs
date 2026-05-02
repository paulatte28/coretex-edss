using System.Diagnostics;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditLoggingService _auditLog;
        private readonly IEmailSender _emailSender;
        private readonly SecurityService _security;
        private readonly GeolocationService _geo;

        public HomeController(
            ILogger<HomeController> logger,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            AuditLoggingService auditLog,
            IEmailSender emailSender,
            SecurityService security,
            GeolocationService geo)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLog = auditLog;
            _emailSender = emailSender;
            _security = security;
            _geo = geo;
        }

        public IActionResult Index()
        {
            return View();
        }

        // OBSOLETE - Onboarding is no longer needed (single company, not multi-tenant SaaS)
        public IActionResult Onboarding()
        {
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("ADMIN") || User.IsInRole("BRANCH_ADMIN")) return Redirect("/Admin");
                if (User.IsInRole("FINANCE")) return Redirect("/finance/dashboard");
                if (User.IsInRole("CASHIER")) return Redirect("/cashier/pos");
                if (User.IsInRole("CEO")) return Redirect("/ceo/dashboard");
                return Redirect("/ceo/dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl ?? string.Empty;
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool remember, string? returnUrl = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            
            // --- Suggestion 2: IP Rate Limiting ---
            if (_security.IsRateLimited(ipAddress, "Login"))
            {
                ViewData["LoginError"] = "Too many attempts. Please wait 1 minute.";
                return View();
            }

            var loginId = (email ?? string.Empty).Trim();
            returnUrl ??= string.Empty;
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Email"] = loginId;

            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] = "Email and password are required.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(loginId)
                ?? await _userManager.FindByNameAsync(loginId);

            if (user == null)
            {
                ViewData["LoginError"] = "Incorrect email or password.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, remember, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await ProcessSecurityAlerts(user, ipAddress);
                await _auditLog.LogActivityAsync("LOGIN_SUCCESS", $"User {loginId} logged in successfully.");
                return await RedirectUserByRole(user, returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                await ProcessSecurityAlerts(user, ipAddress);
                
                // Generate and send OTP
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                await _emailSender.SendEmailAsync(user.Email!, "Your Security Code", 
                    $"Your One-Time Password (OTP) is: <b>{token}</b>. This code expires in 3 minutes.");

                await _auditLog.LogActivityAsync("LOGIN_2FA_CHALLENGE", $"OTP Sent to {loginId}");
                return RedirectToAction("VerifyOTP", new { email = loginId, rememberMe = remember, returnUrl });
            }

            ViewData["LoginError"] = result.IsLockedOut
                ? "Account locked. Contact administrator."
                : result.IsNotAllowed
                    ? "Sign-in is not allowed for this account."
                    : "Incorrect email or password.";

            await _auditLog.LogActivityAsync("LOGIN_FAILURE", $"Failed login attempt for {loginId}");
            return View();
        }

        [HttpGet]
        public IActionResult VerifyOTP(string email, bool rememberMe, string? returnUrl = null)
        {
            ViewData["Email"] = email;
            ViewData["RememberMe"] = rememberMe;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResendOTP(string email, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                await _emailSender.SendEmailAsync(user.Email!, "Your New Security Code", 
                    $"Your new One-Time Password (OTP) is: <b>{token}</b>. It will expire shortly.");
                
                await _auditLog.LogActivityAsync("LOGIN_2FA_RESEND", $"User {email} requested a new OTP.");
                TempData["Message"] = "A new security code has been sent.";
            }

            return RedirectToAction("VerifyOTP", new { email, returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _auditLog.LogActivityAsync("LOGOUT", "User logged out.");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerifyOTP(string email, string code, bool rememberMe, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var result = await _signInManager.TwoFactorSignInAsync("Email", code, rememberMe, rememberClient: false);
            if (result.Succeeded)
            {
                await _auditLog.LogActivityAsync("LOGIN_2FA_SUCCESS", $"User {email} verified OTP successfully.");
                return await RedirectUserByRole(user, returnUrl);
            }

            if (result.IsLockedOut)
            {
                ViewData["Error"] = "Account locked due to too many failed attempts.";
                return View();
            }

            ViewData["Error"] = "Invalid security code. Please try again.";
            ViewData["Email"] = email;
            ViewData["RememberMe"] = rememberMe;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        private async Task<IActionResult> RedirectUserByRole(AppUser user, string? returnUrl)
        {
            if (await _userManager.IsInRoleAsync(user, "ADMIN")) return Redirect("/Admin");
            if (await _userManager.IsInRoleAsync(user, "BRANCH_ADMIN")) return Redirect("/Admin");
            if (await _userManager.IsInRoleAsync(user, "FINANCE")) return Redirect("/finance/dashboard");
            if (await _userManager.IsInRoleAsync(user, "CASHIER")) return Redirect("/cashier/pos");
            if (await _userManager.IsInRoleAsync(user, "CEO")) return Redirect("/ceo/dashboard");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/ceo/dashboard");
        }

        private async Task ProcessSecurityAlerts(AppUser user, string ipAddress)
        {
            var geo = await _geo.GetLocationAsync(ipAddress);
            var currentLocation = $"{geo.City}, {geo.CountryName}";

            // --- Suggestion 1: Geolocation Alert ---
            if (!string.IsNullOrEmpty(user.LastLoginLocation) && user.LastLoginLocation != currentLocation)
            {
                await _emailSender.SendEmailAsync(user.Email!, "SECURITY ALERT: New Login Location", 
                    $"We detected a login to your Coretex account from a new location: <b>{currentLocation}</b>. " +
                    $"Your previous login was from: <b>{user.LastLoginLocation}</b>. If this wasn't you, please reset your password immediately.");
                
                await _auditLog.LogActivityAsync("SECURITY_ALERT", $"New location detected for {user.Email}: {currentLocation}");
            }

            user.LastLoginIP = ipAddress;
            user.LastLoginLocation = currentLocation;
            await _userManager.UpdateAsync(user);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
