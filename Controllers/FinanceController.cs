using coretex_finalproj.Data;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "FINANCE,ADMIN")]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLoggingService _auditLog;
        private readonly ExchangeRateService _exchangeRateService;

        public FinanceController(ApplicationDbContext context, AuditLoggingService auditLog, ExchangeRateService exchangeRateService)
        {
            _context = context;
            _auditLog = auditLog;
            _exchangeRateService = exchangeRateService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            
            var query = _context.Expenses.Include(e => e.Branch).AsQueryable();
            
            // SECURITY ISOLATION: Only show branch-specific data unless Global Admin
            if (user?.BranchId != null && !User.IsInRole("ADMIN"))
            {
                query = query.Where(e => e.BranchId == user.BranchId);
            }

            var expenses = await query.OrderByDescending(e => e.Date).Take(10).ToListAsync();
            return View(expenses);
        }

        [HttpGet]
        public async Task<IActionResult> ExpensesByBranch(Guid? branchId, string search)
        {
            var query = _context.Expenses.Include(e => e.Branch).AsQueryable();
            
            if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId.Value);
            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Description.Contains(search) || e.Category.Contains(search));
            }

            ViewBag.SearchTerm = search;
            return View(await query.OrderByDescending(e => e.Date).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] Expense expense)
        {
            if (expense == null) return BadRequest("Invalid expense data.");

            // Get the logged-in user's branch
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("User not assigned to a branch.");
            
            expense.BranchId = user.BranchId.Value;

            if (ModelState.IsValid)
            {
                // Convert to PHP if necessary before saving
                if (expense.Currency != "PHP")
                {
                    decimal convertedAmount = await _exchangeRateService.ConvertToPhpAsync(expense.Currency, expense.Amount);
                    // Add an audit note about the conversion
                    await _auditLog.LogActivityAsync("CURRENCY_CONVERSION", $"Converted {expense.Amount:C} {expense.Currency} to {convertedAmount:C} PHP for Expense: {expense.Description}");
                    
                    expense.Amount = convertedAmount;
                    expense.Currency = "PHP"; // Save as normalized PHP
                }

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("EXPENSE_CREATE", $"Created expense: {expense.Description} for {expense.Amount:C} PHP", user.BranchId);
                return Json(new { success = true, id = expense.Id, amount = expense.Amount });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpGet]
        [AllowAnonymous] // Allow frontend script to fetch without antiforgery token issues easily
        public async Task<IActionResult> GetExchangeRate(string currency)
        {
            if (string.IsNullOrEmpty(currency) || currency.ToUpper() == "PHP")
                return Json(new { rate = 1.0 });

            // Using $1 to trick our service to just get the raw rate
            decimal rate = await _exchangeRateService.ConvertToPhpAsync(currency, 1.0m);
            return Json(new { rate = rate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExpense(Expense expense)
        {
            if (ModelState.IsValid)
            {
                _context.Expenses.Update(expense);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("EXPENSE_UPDATE", $"Updated expense ID: {expense.Id}");
                return RedirectToAction(nameof(Submissions));
            }
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveExpense(Guid id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                expense.IsArchived = true;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("EXPENSE_ARCHIVE", $"Archived expense: {expense.Description}");
            }
            return RedirectToAction(nameof(Submissions));
        }

        public IActionResult ExpensesCogs() => View();
        public IActionResult ExpensesRent() => View();
        public IActionResult ExpensesSalaries() => View();
        public IActionResult ExpensesUtilities() => View();
        public IActionResult Review() => View();
        public IActionResult Submit() => View();
        public IActionResult EditSubmission() => View();
        public IActionResult Submissions() => View();
        [HttpPost]
        public async Task<IActionResult> SubmitMonth(int year, int month, string notes)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            // Check if already submitted
            var existing = await _context.BranchSubmissions
                .AnyAsync(s => s.BranchId == user.BranchId && s.SubmissionYear == year && s.SubmissionMonth == month);
            
            if (existing) return BadRequest("Month already submitted.");

            // Calculate totals from database
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var totalSales = await _context.Sales
                .Where(s => s.BranchId == user.BranchId && s.Date >= startDate && s.Date < endDate && !s.IsArchived)
                .SumAsync(s => s.Amount);

            var totalExpenses = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived)
                .SumAsync(e => e.Amount);

            // Calculate Breakdown for the Submission record
            var cogs = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived && e.Category == "COGS")
                .SumAsync(e => e.Amount);
            var rent = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived && e.Category == "Rent")
                .SumAsync(e => e.Amount);
            var salaries = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived && e.Category == "Salaries")
                .SumAsync(e => e.Amount);
            var utilities = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived && e.Category == "Utilities")
                .SumAsync(e => e.Amount);

            var submission = new BranchSubmission
            {
                BranchId = user.BranchId.Value,
                SubmissionYear = year,
                SubmissionMonth = month,
                SubmittedByUserId = user.Id,
                SubmittedAt = DateTime.Now,
                SalesRevenue = totalSales,
                Expenses = totalExpenses,
                Cogs = cogs,
                Rent = rent,
                Salaries = salaries,
                Utilities = utilities,
                Status = "Submitted",
                Notes = notes ?? ""
            };

            _context.BranchSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync("MONTH_SUBMIT", $"Finalized submission for {year}-{month:D2}. Revenue: {totalSales:C}, Expenses: {totalExpenses:C}");

            return Json(new { success = true, submissionId = submission.Id });
        }
        [HttpGet]
        public async Task<IActionResult> GetFinanceSnapshot(int year, int month)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var sales = await _context.Sales
                .Where(s => s.BranchId == user.BranchId && s.Date >= startDate && s.Date < endDate && !s.IsArchived)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.BranchId == user.BranchId && e.Date >= startDate && e.Date < endDate && !e.IsArchived)
                .ToListAsync();

            var submission = await _context.BranchSubmissions
                .FirstOrDefaultAsync(s => s.BranchId == user.BranchId && s.SubmissionYear == year && s.SubmissionMonth == month);

            var totalRevenue = sales.Sum(s => s.Amount);
            var totalTransactions = sales.Count;
            
            var expenseBreakdown = new
            {
                cogs = expenses.Where(e => e.Category == "COGS").Sum(e => e.Amount),
                rent = expenses.Where(e => e.Category == "Rent").Sum(e => e.Amount),
                salaries = expenses.Where(e => e.Category == "Salaries").Sum(e => e.Amount),
                utilities = expenses.Where(e => e.Category == "Utilities").Sum(e => e.Amount),
                total = expenses.Sum(e => e.Amount)
            };

            var topProduct = sales.GroupBy(s => s.ProductName)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Qty = g.Count() })
                .FirstOrDefault();

            return Json(new
            {
                totalRevenue,
                totalTransactions,
                expenseBreakdown,
                netProfit = totalRevenue - expenseBreakdown.total,
                topProduct = topProduct?.Name ?? "-",
                topProductQty = topProduct?.Qty ?? 0,
                submissionStatus = submission?.Status ?? "Not Submitted",
                isLocked = submission != null,
                submissionDate = submission?.SubmittedAt
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetSubmissionHistory()
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var history = await _context.BranchSubmissions
                .Where(s => s.BranchId == user.BranchId)
                .OrderByDescending(s => s.SubmissionYear)
                .ThenByDescending(s => s.SubmissionMonth)
                .Select(s => new {
                    id = s.Id,
                    month = $"{s.SubmissionYear}-{s.SubmissionMonth:D2}",
                    submittedAt = s.SubmittedAt,
                    totalSales = s.SalesRevenue,
                    totalExpenses = s.Expenses,
                    netProfit = s.SalesRevenue - s.Expenses,
                    status = s.Status
                })
                .ToListAsync();

            return Json(history);
        }
        [HttpPost]
        public async Task<IActionResult> ResetSubmissions(int year, int month)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var submissions = await _context.BranchSubmissions
                .Where(s => s.BranchId == user.BranchId && s.SubmissionYear == year && s.SubmissionMonth == month)
                .ToListAsync();

            if (submissions.Any())
            {
                _context.BranchSubmissions.RemoveRange(submissions);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("MONTH_RESET", $"CEO/Finance reset the submission for {year}-{month:D2} to allow re-entry.");
            }

            return Json(new { success = true });
        }
    }
}
