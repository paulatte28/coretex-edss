using coretex_finalproj.Data;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CASHIER,ADMIN")]
    public class CashierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLoggingService _auditLog;

        public CashierController(ApplicationDbContext context, AuditLoggingService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<IActionResult> Pos()
        {
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            var sales = await _context.Sales.Where(s => s.Date >= DateTime.Today).ToListAsync();
            return View(sales);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] Sale sale)
        {
            if (sale == null) return BadRequest("Invalid sale data.");

            // Get the logged-in user's branch
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("User not assigned to a branch.");

            // Auto-Generate Order ID (CTX-YYYY-XXXX)
            var count = await _context.Sales.CountAsync() + 1;
            sale.OrderId = $"CTX-{DateTime.Now.Year}-{count:D4}";
            sale.Date = DateTime.Now;
            sale.BranchId = user.BranchId.Value;

            if (ModelState.IsValid)
            {
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SALE_CREATE", $"Created sale {sale.OrderId} for {sale.Amount:C}");
                return Json(new { success = true, orderId = sale.OrderId, amount = sale.Amount });
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveSale(Guid id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null)
            {
                sale.IsArchived = true;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SALE_ARCHIVE", $"Archived sale {sale.OrderId}");
            }
            return RedirectToAction(nameof(Pos));
        }

        public IActionResult DailySummary()
        {
            return View();
        }

        public async Task<IActionResult> Transactions(string search)
        {
            var query = _context.Sales.Include(s => s.Branch).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.OrderId.Contains(search) || s.CustomerName.Contains(search));
            }

            ViewBag.SearchTerm = search;
            var sales = await query.OrderByDescending(s => s.Date).ToListAsync();
            return View(sales);
        }
        [HttpGet]
        public async Task<IActionResult> GetDailySummary(DateTime date)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var sales = await _context.Sales
                .Where(s => s.BranchId == user.BranchId && s.Date >= startDate && s.Date < endDate && !s.IsArchived)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            var totalSales = sales.Sum(s => s.Amount);
            var topProduct = sales.GroupBy(s => s.ProductName)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .FirstOrDefault();

            return Json(new
            {
                totalTransactions = sales.Count,
                totalSales,
                topProductName = topProduct?.Name ?? "-",
                topProductQty = topProduct?.Qty ?? 0,
                transactions = sales.Select(s => new {
                    productName = s.ProductName,
                    quantity = s.Quantity,
                    unitPrice = s.UnitPrice,
                    lineTotal = s.Amount,
                    timestamp = s.Date
                })
            });
        }
    }
}