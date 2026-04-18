using coretex_finalproj.Data;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Services
{
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetMonthlyProfitLossAsync()
        {
            var sales = await _context.Sales
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.Amount) })
                .ToListAsync();

            var expenses = await _context.Expenses
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => e.Amount) })
                .ToListAsync();

            var months = sales.Select(s => new { s.Year, s.Month })
                .Union(expenses.Select(e => new { e.Year, e.Month }))
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return months.Select(m => {
                var monthSales = sales.FirstOrDefault(s => s.Year == m.Year && s.Month == m.Month)?.Total ?? 0;
                var monthExpenses = expenses.FirstOrDefault(e => e.Year == m.Year && e.Month == m.Month)?.Total ?? 0;
                return new {
                    Month = $"{m.Year}-{m.Month:D2}",
                    Sales = monthSales,
                    Expenses = monthExpenses,
                    Profit = monthSales - monthExpenses
                };
            });
        }

        public async Task<object> GetBranchPerformanceAsync()
        {
            return await _context.Branches
                .Select(b => new {
                    BranchName = b.Name,
                    TotalSales = _context.Sales.Where(s => s.BranchId == b.Id).Sum(s => s.Amount),
                    TotalExpenses = _context.Expenses.Where(e => e.BranchId == b.Id).Sum(e => e.Amount)
                })
                .Select(b => new {
                    b.BranchName,
                    b.TotalSales,
                    b.TotalExpenses,
                    Profit = b.TotalSales - b.TotalExpenses
                })
                .ToListAsync();
        }

        public async Task<object> GetExpenseCategoriesAsync()
        {
            return await _context.Expenses
                .GroupBy(e => e.Category)
                .Select(g => new {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount)
                })
                .ToListAsync();
        }
    }
}
