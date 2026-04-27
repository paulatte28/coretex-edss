using coretex_finalproj.Data;
using coretex_finalproj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register generic API services
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<GeolocationService>();
builder.Services.AddScoped<AuditLoggingService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<SummaryProcessingService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<TrendService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();

builder.Services
    .AddDefaultIdentity<AppUser>(options =>
    {
        // 1. Account Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // 2. Password Complexity (NIST Standards)
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 12; // Standard: 12+ characters
        options.Password.RequiredUniqueChars = 1;

        // 3. 2FA & Confirmation
        options.SignIn.RequireConfirmedAccount = true; 
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Real-time Security: Logout after 20 mins of inactivity
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true; // Protects against XSS cookie theft
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Changed for local testing (HTTP)
    options.Cookie.SameSite = SameSiteMode.Lax; // Relaxed for local redirects
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedDataAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
app.Use(async (context, next) =>
{
    // Content Security Policy (CSP) - "Real" industry security
    // Prevents XSS by only allowing scripts from trusted sources
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.plot.ly https://cdn.tailwindcss.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdn.tailwindcss.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "frame-ancestors 'none';"; // Protects against Clickjacking

    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

// FORCE DEVELOPER ERRORS TO SHOW (TEMPORARY FOR DEBUGGING)
app.UseDeveloperExceptionPage();
app.UseMigrationsEndPoint();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "cashier_pos",
    pattern: "cashier/pos",
    defaults: new { controller = "Cashier", action = "Pos" });

app.MapControllerRoute(
    name: "cashier_daily_summary",
    pattern: "cashier/daily-summary",
    defaults: new { controller = "Cashier", action = "DailySummary" });

app.MapControllerRoute(
    name: "cashier_transactions",
    pattern: "cashier/transactions",
    defaults: new { controller = "Cashier", action = "Transactions" });

app.MapControllerRoute(
    name: "finance_dashboard",
    pattern: "finance/dashboard",
    defaults: new { controller = "Finance", action = "Dashboard" });

app.MapControllerRoute(
    name: "finance_expenses_cogs",
    pattern: "finance/expenses/cogs",
    defaults: new { controller = "Finance", action = "ExpensesCogs" });

app.MapControllerRoute(
    name: "finance_expenses_rent",
    pattern: "finance/expenses/rent",
    defaults: new { controller = "Finance", action = "ExpensesRent" });

app.MapControllerRoute(
    name: "finance_expenses_salaries",
    pattern: "finance/expenses/salaries",
    defaults: new { controller = "Finance", action = "ExpensesSalaries" });

app.MapControllerRoute(
    name: "finance_expenses_utilities",
    pattern: "finance/expenses/utilities",
    defaults: new { controller = "Finance", action = "ExpensesUtilities" });

app.MapControllerRoute(
    name: "finance_review",
    pattern: "finance/review",
    defaults: new { controller = "Finance", action = "Review" });

app.MapControllerRoute(
    name: "finance_submit",
    pattern: "finance/submit",
    defaults: new { controller = "Finance", action = "Submit" });

app.MapControllerRoute(
    name: "finance_edit_submission",
    pattern: "finance/edit-submission",
    defaults: new { controller = "Finance", action = "EditSubmission" });

app.MapControllerRoute(
    name: "finance_submissions",
    pattern: "finance/submissions",
    defaults: new { controller = "Finance", action = "Submissions" });

app.MapControllerRoute(
    name: "ceo_root",
    pattern: "ceo",
    defaults: new { controller = "Ceo", action = "Index" });

app.MapControllerRoute(
    name: "ceo_dashboard",
    pattern: "ceo/dashboard",
    defaults: new { controller = "Ceo", action = "Dashboard" });

app.MapControllerRoute(
    name: "ceo_kpi_profit_margin",
    pattern: "ceo/kpi/profit-margin",
    defaults: new { controller = "Ceo", action = "KpiProfitMargin" });

app.MapControllerRoute(
    name: "ceo_kpi_expense_ratio",
    pattern: "ceo/kpi/expense-ratio",
    defaults: new { controller = "Ceo", action = "KpiExpenseRatio" });

app.MapControllerRoute(
    name: "ceo_analytics_forecast",
    pattern: "ceo/analytics/forecast",
    defaults: new { controller = "Ceo", action = "AnalyticsForecast" });

app.MapControllerRoute(
    name: "ceo_analytics_expense_trend",
    pattern: "ceo/analytics/expense-trend",
    defaults: new { controller = "Ceo", action = "AnalyticsExpenseTrend" });

app.MapControllerRoute(
    name: "ceo_kpi_risk_score",
    pattern: "ceo/kpi/risk-score",
    defaults: new { controller = "Ceo", action = "KpiRiskScore" });

app.MapControllerRoute(
    name: "ceo_analytics_health_summary",
    pattern: "ceo/analytics/health-summary",
    defaults: new { controller = "Ceo", action = "AnalyticsHealthSummary" });

app.MapControllerRoute(
    name: "ceo_branches_compare",
    pattern: "ceo/branches/compare",
    defaults: new { controller = "Ceo", action = "BranchesCompare" });

app.MapControllerRoute(
    name: "ceo_analytics_mom",
    pattern: "ceo/analytics/mom",
    defaults: new { controller = "Ceo", action = "AnalyticsMom" });

app.MapControllerRoute(
    name: "ceo_charts",
    pattern: "ceo/charts",
    defaults: new { controller = "Ceo", action = "Charts" });

app.MapControllerRoute(
    name: "ceo_analytics_predictive",
    pattern: "ceo/analytics/predictive",
    defaults: new { controller = "Ceo", action = "AnalyticsPredictive" });

app.MapControllerRoute(
    name: "ceo_news",
    pattern: "ceo/news",
    defaults: new { controller = "Ceo", action = "News" });

app.MapControllerRoute(
    name: "ceo_reports_generate",
    pattern: "ceo/reports/generate",
    defaults: new { controller = "Ceo", action = "ReportsGenerate" });

app.MapControllerRoute(
    name: "ceo_reports",
    pattern: "ceo/reports",
    defaults: new { controller = "Ceo", action = "Reports" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
