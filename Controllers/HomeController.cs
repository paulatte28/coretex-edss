using System.Diagnostics;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly coretex_finalproj.Data.ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public HomeController(
            ILogger<HomeController> logger,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            AuditLoggingService auditLog,
            IEmailSender emailSender,
            SecurityService security,
            GeolocationService geo,
            coretex_finalproj.Data.ApplicationDbContext context,
            NotificationService notificationService)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLog = auditLog;
            _emailSender = emailSender;
            _security = security;
            _geo = geo;
            _context = context;
            _notificationService = notificationService;
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
                
                // FALLBACK: If authenticated but no role found, log out to clear the stale session
                return RedirectToAction("Logout", "Home");
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

            var user = await _userManager.Users.Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Email == loginId || u.UserName == loginId);

            if (user == null)
            {
                ViewData["LoginError"] = "Incorrect email or password.";
                return View();
            }

            // --- SECURITY ENFORCEMENT: Branch Node Integrity ---
            if (user.BranchId != null)
            {
                var isElevated = await _userManager.IsInRoleAsync(user, "ADMIN") || await _userManager.IsInRoleAsync(user, "CEO");
                if (!isElevated)
                {
                    if (user.Branch == null || !user.Branch.IsActive || user.Branch.IsArchived)
                    {
                        await _auditLog.LogActivityAsync("LOGIN_BLOCKED", $"User {loginId} blocked: Branch {user.Branch?.Name ?? "Unknown"} is Offline/Archived.");
                        ViewData["LoginError"] = "ACCESS DENIED: Your assigned operational node is currently in Maintenance Mode or has been Archived.";
                        return View();
                    }
                }
            }

            // --- SECURITY ENFORCEMENT: Enforce 2FA for ALL users ---
            if (!user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
                await _auditLog.LogActivityAsync("SECURITY_ENFORCE", $"MFA automatically enforced for {loginId} during login.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, remember, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                await ProcessSecurityAlerts(user, ipAddress);
                await _auditLog.LogActivityAsync("LOGIN_SUCCESS", $"User {loginId} logged in.");
                
                // FORCE COOKIE REFRESH
                await _signInManager.RefreshSignInAsync(user);
                return await RedirectUserByRole(user, returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                // --- SECURITY UPGRADE: PIN UNIQUENESS ---
                // Rotate the security stamp BEFORE generating the token.
                // This forces the system to create a BRAND NEW, unique PIN every single time
                // a user hits the login screen, regardless of the expiration window.
                await _userManager.UpdateSecurityStampAsync(user);

                // 1. Generate PIN
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                
                // 2. Send via Email (Professional Template)
                await _emailSender.SendEmailAsync(user.Email!, "Security Verification Code", 
                    $"<div style='font-family:sans-serif; padding:30px; border:2px solid #e2e8f0; border-radius:24px; background-color:white;'>" +
                    $"<h2 style='color:#001A4D; margin-top:0;'>Security Verification</h2>" +
                    $"<p style='color:#64748b;'>Use the following code to complete your login to Coretex:</p>" +
                    $"<div style='background:#f1f5f9; padding:20px; border-radius:16px; text-align:center; margin:20px 0;'>" +
                    $"<span style='font-size:32px; font-weight:800; letter-spacing:8px; color:#006D68;'>{token}</span>" +
                    $"</div>" +
                    $"<p style='font-size:12px; color:#94a3b8;'>This code expires in 3 minutes. If you did not request this, please secure your account.</p>" +
                    $"</div>");

                await _auditLog.LogActivityAsync("LOGIN_2FA_CHALLENGE", $"PIN Challenge issued for {loginId}. OTP sent via email.");
                
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
                // --- CRITICAL SECURITY FIX: PIN CONSUMPTION ---
                // Rotate the security stamp to invalidate the used PIN immediately.
                // This prevents "Replay Attacks" where the same code is used twice.
                await _userManager.UpdateSecurityStampAsync(user);
                
                // RESET STRIKES: Clear the failed attempts count since they finally got it right
                await _userManager.ResetAccessFailedCountAsync(user);
                
                // --- TACTICAL UPGRADE: Node-Lock Verification ---
                var geo = await _geo.GetLocationAsync(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                string loginStatus = "LOGIN_SUCCESS";
                string loginDescription = $"User authenticated successfully: {email} from {geo.City}, {geo.CountryName}";

                // If the user belongs to a branch, check if the login city matches the branch region
                if (user.BranchId.HasValue)
                {
                    var branch = await _context.Branches.FindAsync(user.BranchId.Value);
                    if (branch != null && !string.IsNullOrEmpty(geo.City) && geo.City != "Unknown")
                    {
                        // Check if the branch name or address contains the city name (fuzzy match)
                        bool isMatch = branch.Name.Contains(geo.City, StringComparison.OrdinalIgnoreCase) || 
                                      branch.Address.Contains(geo.City, StringComparison.OrdinalIgnoreCase);
                        
                        if (!isMatch)
                        {
                            loginStatus = "SECURITY_ALERT";
                            loginDescription = $"UNUSUAL_LOCATION: {email} (Assigned: {branch.Name}) logged in from {geo.City}. Potential account compromise.";
                            
                            // Also trigger a system notification for the Admin
                            await _notificationService.CreateNotificationAsync(
                                user.BranchId.Value, 
                                "SECURITY: Unusual Login Node", 
                                $"Account {email} accessed from {geo.City} (Expected: {branch.Name}).", 
                                "red"
                            );
                        }
                    }
                }

                await _auditLog.LogActivityAsync(loginStatus, loginDescription);
                
                // ENSURE FRESH PRINCIPAL: Sync the new security stamp into the cookie
                await _signInManager.RefreshSignInAsync(user);
                return await RedirectUserByRole(user, returnUrl);
            }

            if (result.IsLockedOut)
            {
                ViewData["Email"] = email;
                ViewData["RememberMe"] = rememberMe;
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["Error"] = "Your account is temporarily locked due to too many failed attempts. Please try again in 30 minutes.";
                return View();
            }

            // --- REALISTIC SECURITY: Failed PIN Attempt Tracking ---
            await _userManager.AccessFailedAsync(user);
            int failedCount = await _userManager.GetAccessFailedCountAsync(user);
            int maxAttempts = 5; 

            // --- TACTICAL UPGRADE: Response Throttling ---
            // If the user has failed 3+ times, we intentionally slow down the response 
            // to choke brute-force bots. 2 seconds for 3rd try, 4 seconds for 4th try.
            if (failedCount >= 3)
            {
                await Task.Delay(2000 * (failedCount - 2));
            }

            if (failedCount >= maxAttempts)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(30));
                await _auditLog.LogActivityAsync("ACCOUNT_LOCKOUT", $"Account {email} locked out after {failedCount} failed PIN attempts.");
                ViewData["Error"] = "Too many failed attempts. Your account has been locked for 30 minutes.";
            }
            else
            {
                ViewData["Error"] = $"Invalid security code. You have {maxAttempts - failedCount} attempts remaining.";
            }

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

            return RedirectToAction("Logout", "Home");
        }

        private async Task ProcessSecurityAlerts(AppUser user, string ipAddress)
        {
            var geo = await _geo.GetLocationAsync(ipAddress);
            var currentLocation = $"{geo.City}, {geo.CountryName}";

         
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
