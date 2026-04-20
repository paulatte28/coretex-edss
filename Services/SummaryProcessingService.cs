using coretex_finalproj.Data;
using coretex_finalproj.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class SummaryProcessingService
    {
        private readonly ApplicationDbContext _context;

        public SummaryProcessingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessDailySummariesAsync(DateTime targetDate)
        {
            var dateOnly = targetDate.Date;
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();

            foreach (var branch in branches)
            {
                var sales = await _context.Sales
                    .Where(s => s.BranchId == branch.Id && s.Date.Date == dateOnly && !s.IsArchived)
                    .ToListAsync();

                var expenses = await _context.Expenses
                    .Where(e => e.BranchId == branch.Id && e.Date.Date == dateOnly && !e.IsArchived)
                    .ToListAsync();

                var totalRevenue = sales.Sum(s => s.Amount);
                var totalExpenses = expenses.Sum(e => e.Amount);

                var summary = await _context.DailySummaries
                    .FirstOrDefaultAsync(d => d.BranchId == branch.Id && d.Date.Date == dateOnly);

                if (summary == null)
                {
                    summary = new DailySummary
                    {
                        Date = dateOnly,
                        BranchId = branch.Id
                    };
                    _context.DailySummaries.Add(summary);
                }

                summary.TotalRevenue = totalRevenue;
                summary.TotalExpenses = totalExpenses;
                summary.NetProfit = totalRevenue - totalExpenses;
                summary.TransactionCount = sales.Count;
            }

            await _context.SaveChangesAsync();
        }
    }
}
