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
            var sales = await _context.Sales.Where(s => s.Date >= DateTime.Today).ToListAsync();
            return View(sales);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSale(Sale sale)
        {
            if (ModelState.IsValid)
            {
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SALE_CREATE", $"Created sale {sale.OrderId} for {sale.Amount:C}");
                return RedirectToAction(nameof(Pos));
            }
            return View(nameof(Pos), await _context.Sales.Where(s => s.Date >= DateTime.Today).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSaleStatus(Guid id, string status)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null)
            {
                sale.Status = status;
                await _context.SaveChangesAsync();
                await _auditLog.LogActivityAsync("SALE_STATUS_UPDATE", $"Updated sale {sale.OrderId} status to {status}");
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
    }
}