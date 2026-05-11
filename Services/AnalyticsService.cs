using coretex_finalproj.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSnapshot> GetDashboardSnapshotAsync(Guid? branchId = null)
        {
            var salesQuery = _context.Sales.Where(s => !s.IsArchived).AsQueryable();
            var expensesQuery = _context.Expenses.Where(e => !e.IsArchived).AsQueryable();

            if (branchId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
            }

            // Real data from DB
            var realSales = await salesQuery.SumAsync(s => s.Amount);
            var realExpenses = await expensesQuery.SumAsync(e => e.Amount);
            
            var totalSales = realSales;
            var totalExpenses = realExpenses;
            var profit = totalSales - totalExpenses;
            var margin = totalSales > 0 ? (profit / totalSales) * 100 : 0;

            var threshold = await _context.KpiThresholds.FirstOrDefaultAsync(t => t.IsActive);
            var minMargin = threshold?.MinProfitMargin ?? 15m;
            
            var riskLevel = "Healthy";
            if (margin < minMargin) riskLevel = "Warning";
            if (profit < 0) riskLevel = "Critical";

            return new DashboardSnapshot
            {
                TotalRevenue = totalSales,
                TotalExpenses = totalExpenses,
                NetProfit = profit,
                ProfitMargin = margin,
                RiskLevel = riskLevel
            };
        }

        public async Task<object> GetMonthlyProfitLossAsync(Guid? branchId = null)
        {
            var data = new List<object>();
            var now = DateTime.Now;

            // FETCH DATA FOR THE LAST 6 MONTHS
            for (int i = 5; i >= 0; i--)
            {
                var d = now.AddMonths(-i);
                var start = new DateTime(d.Year, d.Month, 1);
                var end = start.AddMonths(1);

                // 1. CHECK FOR OFFICIAL SUBMISSIONS FIRST (Higher Priority/Confirmed Data)
                var submission = await _context.BranchSubmissions
                    .Where(s => s.SubmissionYear == d.Year && s.SubmissionMonth == d.Month)
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .FirstOrDefaultAsync();

                decimal monthlySales = 0;
                decimal monthlyExpenses = 0;

                if (submission != null)
                {
                    // Use confirmed data from the report
                    monthlySales = submission.SalesRevenue;
                    monthlyExpenses = submission.Expenses;
                }
                else
                {
                    // 2. FALLBACK TO RAW TRANSACTIONAL DATA (Real-time/In-progress)
                    var salesQuery = _context.Sales.Where(s => !s.IsArchived && s.Date >= start && s.Date < end).AsQueryable();
                    var expensesQuery = _context.Expenses.Where(e => !e.IsArchived && e.Date >= start && e.Date < end).AsQueryable();

                    if (branchId.HasValue)
                    {
                        salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                        expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
                    }

                    monthlySales = await salesQuery.SumAsync(s => s.Amount);
                    monthlyExpenses = await expensesQuery.SumAsync(e => e.Amount);
                }

                data.Add(new {
                    month = d.ToString("MMM yyyy") + (i == 0 ? " (LIVE)" : ""),
                    sales = monthlySales,
                    expenses = monthlyExpenses
                });
            }

            return data;
        }

        public async Task<List<object>> GetBranchPerformanceAsync()
        {
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            var results = new List<object>();

            foreach (var b in branches)
            {
                // 1. SUM ALL OFFICIAL SUBMISSIONS
                var submissionTotals = await _context.BranchSubmissions
                    .Where(s => s.BranchId == b.Id)
                    .Select(s => new { s.SalesRevenue, s.Expenses, s.SubmissionYear, s.SubmissionMonth })
                    .ToListAsync();

                decimal totalRevenue = submissionTotals.Sum(s => s.SalesRevenue);
                decimal totalExpenses = submissionTotals.Sum(s => s.Expenses);

                // 2. SUM LIVE DATA FOR MONTHS WITHOUT SUBMISSIONS
                // We'll just look at Sales/Expenses for any date that isn't covered by a submission month.
                // For simplicity in this demo, we'll sum all Sales and then subtract those that belong to submitted months.
                var allLiveSales = await _context.Sales.Where(s => s.BranchId == b.Id && !s.IsArchived).ToListAsync();
                var allLiveExpenses = await _context.Expenses.Where(e => e.BranchId == b.Id && !e.IsArchived).ToListAsync();

                foreach (var s in allLiveSales)
                {
                    // If this sale's month isn't in the submissions list, add it to the total
                    if (!submissionTotals.Any(sub => sub.SubmissionYear == s.Date.Year && sub.SubmissionMonth == s.Date.Month))
                    {
                        totalRevenue += s.Amount;
                    }
                }

                foreach (var e in allLiveExpenses)
                {
                    if (!submissionTotals.Any(sub => sub.SubmissionYear == e.Date.Year && sub.SubmissionMonth == e.Date.Month))
                    {
                        totalExpenses += e.Amount;
                    }
                }

                var profit = totalRevenue - totalExpenses;
                var margin = totalRevenue > 0 ? (double)(profit / totalRevenue) * 100 : 0;

                results.Add(new {
                    Name = b.Name,
                    Revenue = totalRevenue,
                    Expenses = totalExpenses,
                    Profit = profit,
                    Margin = margin
                });
            }

            return results.OrderByDescending(r => (decimal)((dynamic)r).Revenue).ToList();
        }

        public async Task<object> GetExpenseCategoriesAsync(Guid? branchId = null)
        {
            var query = _context.Expenses.Where(e => !e.IsArchived).AsQueryable();
            if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId.Value);

            var categories = await query
                .GroupBy(e => e.Category)
                .Select(g => new { 
                    category = g.Key, 
                    amount = g.Sum(e => e.Amount) 
                })
                .ToListAsync();

            // If the breakdown is empty, try to pull the total from the latest submission
            if (categories.Count == 0)
            {
                var latestSubmission = await _context.BranchSubmissions
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .OrderByDescending(s => s.SubmittedAt)
                    .FirstOrDefaultAsync();

                if (latestSubmission != null && latestSubmission.Expenses > 0)
                {
                    return new List<object> { 
                        new { category = "COGS", amount = latestSubmission.Cogs },
                        new { category = "Rent", amount = latestSubmission.Rent },
                        new { category = "Salaries", amount = latestSubmission.Salaries },
                        new { category = "Utilities", amount = latestSubmission.Utilities },
                        new { category = "Misc", amount = latestSubmission.Expenses - (latestSubmission.Cogs + latestSubmission.Rent + latestSubmission.Salaries + latestSubmission.Utilities) }
                    }.Where(x => (decimal)((dynamic)x).amount > 0).ToList();
                }
            }

            return categories;
        }

        public async Task<decimal> GetSalesForecastAsync(Guid? branchId)
        {
            var today = DateTime.Today;
            var threeMonthsAgo = today.AddMonths(-3);

            var pastSales = await _context.Sales
                .Where(s => !s.IsArchived && s.Date >= threeMonthsAgo)
                .Where(s => !branchId.HasValue || s.BranchId == branchId)
                .OrderBy(s => s.Date)
                .ToListAsync();

            if (pastSales.Count < 2) return 0;

            // Simple average daily sales * 30 days
            var totalDays = (pastSales.Max(s => s.Date) - pastSales.Min(s => s.Date)).TotalDays;
            if (totalDays <= 0) return 0;

            var dailyAvg = pastSales.Sum(s => s.Amount) / (decimal)totalDays;
            return dailyAvg * 30; // Predicted for next 30 days
        }
    }

    public class DashboardSnapshot
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public string RiskLevel { get; set; } = "Healthy";
    }
}
