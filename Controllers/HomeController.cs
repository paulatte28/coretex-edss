using System.Diagnostics;
using coretex_finalproj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
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
                if (User.IsInRole("ADMIN")) return Redirect("/Admin");
                if (User.IsInRole("FINANCE")) return Redirect("/finance/dashboard");
                if (User.IsInRole("CASHIER")) return Redirect("/cashier/pos");
                if (User.IsInRole("CEO")) return Redirect("/ceo/dashboard");
                return Redirect("/ceo/dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl ?? string.Empty;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool remember, string? returnUrl = null)
        {
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
                if (await _userManager.IsInRoleAsync(user, "ADMIN")) return Redirect("/Admin");
                if (await _userManager.IsInRoleAsync(user, "FINANCE")) return Redirect("/finance/dashboard");
                if (await _userManager.IsInRoleAsync(user, "CASHIER")) return Redirect("/cashier/pos");
                if (await _userManager.IsInRoleAsync(user, "CEO")) return Redirect("/ceo/dashboard");

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return Redirect("/ceo/dashboard");
            }

            ViewData["LoginError"] = result.IsLockedOut
                ? "Account locked. Contact administrator."
                : result.IsNotAllowed
                    ? "Sign-in is not allowed for this account."
                    : "Incorrect email or password.";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
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
