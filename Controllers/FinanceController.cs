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

        public FinanceController(ApplicationDbContext context, AuditLoggingService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var expenses = await _context.Expenses.Include(e => e.Branch).OrderByDescending(e => e.Date).Take(10).ToListAsync();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(Expense expense)
        {
            if (ModelState.IsValid)
            {
                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("EXPENSE_CREATE", $"Created expense: {expense.Description} for {expense.Amount:C}");
                return RedirectToAction(nameof(Submissions));
            }
            return View(expense);
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
    }
}
