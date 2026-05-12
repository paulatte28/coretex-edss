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

        public async Task<DashboardSnapshot> GetDashboardSnapshotAsync(Guid? branchId = null, int? month = null, int? year = null)
        {
            decimal totalSales = 0;
            decimal totalExpenses = 0;

            // If a specific month/year is requested, check for an official submission first
            if (month.HasValue && year.HasValue)
            {
                var submissions = await _context.BranchSubmissions
                    .Where(s => s.SubmissionYear == year.Value && s.SubmissionMonth == month.Value)
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .ToListAsync();

                if (submissions.Any())
                {
                    totalSales = submissions.Sum(s => s.SalesRevenue);
                    totalExpenses = submissions.Sum(s => s.Expenses);
                }
                else
                {
                    // Fallback to raw data for that specific month
                    var start = new DateTime(year.Value, month.Value, 1);
                    var end = start.AddMonths(1);

                    var salesQuery = _context.Sales.Where(s => !s.IsArchived && s.Date >= start && s.Date < end).AsQueryable();
                    var expensesQuery = _context.Expenses.Where(e => !e.IsArchived && e.Date >= start && e.Date < end).AsQueryable();

                    if (branchId.HasValue)
                    {
                        salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                        expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
                    }

                    totalSales = await salesQuery.SumAsync(s => s.Amount);
                    totalExpenses = await expensesQuery.SumAsync(e => e.Amount);
                }
            }
            else
            {
                // DEFAULT: ALL-TIME TOTAL (Live + Historical Reports)
                // 1. Sum all approved submissions
                var reportSales = await _context.BranchSubmissions
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .SumAsync(s => s.SalesRevenue);
                var reportExpenses = await _context.BranchSubmissions
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .SumAsync(s => s.Expenses);

                // 2. Sum live data for months that DON'T have submissions yet
                var submissions = await _context.BranchSubmissions
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .Select(s => new { s.SubmissionYear, s.SubmissionMonth })
                    .ToListAsync();

                var liveSales = await _context.Sales
                    .Where(s => !s.IsArchived && (!branchId.HasValue || s.BranchId == branchId.Value))
                    .ToListAsync();
                var liveExpenses = await _context.Expenses
                    .Where(e => !e.IsArchived && (!branchId.HasValue || e.BranchId == branchId.Value))
                    .ToListAsync();

                foreach(var s in liveSales) {
                    if (!submissions.Any(sub => sub.SubmissionYear == s.Date.Year && sub.SubmissionMonth == s.Date.Month))
                        totalSales += s.Amount;
                }
                foreach(var e in liveExpenses) {
                    if (!submissions.Any(sub => sub.SubmissionYear == e.Date.Year && sub.SubmissionMonth == e.Date.Month))
                        totalExpenses += e.Amount;
                }

                totalSales += reportSales;
                totalExpenses += reportExpenses;
            }
            
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

        public async Task<object> GetMonthlyProfitLossAsync(Guid? branchId = null, int? month = null, int? year = null)
        {
            var data = new List<object>();
            var now = (month.HasValue && year.HasValue) 
                ? new DateTime(year.Value, month.Value, 1).AddMonths(1).AddDays(-1)
                : DateTime.Now;

            // FETCH DATA FOR THE LAST 6 MONTHS
            for (int i = 5; i >= 0; i--)
            {
                var d = now.AddMonths(-i);
                var start = new DateTime(d.Year, d.Month, 1);
                var end = start.AddMonths(1);

                // 1. CHECK FOR OFFICIAL SUBMISSIONS FIRST (Higher Priority/Confirmed Data)
                var submissions = await _context.BranchSubmissions
                    .Where(s => s.SubmissionYear == d.Year && s.SubmissionMonth == d.Month)
                    .Where(s => !branchId.HasValue || s.BranchId == branchId.Value)
                    .ToListAsync();

                decimal monthlySales = 0;
                decimal monthlyExpenses = 0;
                decimal monthlyCogs = 0;
                decimal monthlyRent = 0;
                decimal monthlySalaries = 0;
                decimal monthlyUtilities = 0;

                if (submissions.Any())
                {
                    monthlySales = submissions.Sum(s => s.SalesRevenue);
                    monthlyExpenses = submissions.Sum(s => s.Expenses);
                    monthlyCogs = submissions.Sum(s => s.Cogs);
                    monthlyRent = submissions.Sum(s => s.Rent);
                    monthlySalaries = submissions.Sum(s => s.Salaries);
                    monthlyUtilities = submissions.Sum(s => s.Utilities);
                }
                else
                {
                    var salesQuery = _context.Sales.Where(s => !s.IsArchived && s.Date >= start && s.Date < end).AsQueryable();
                    var expensesQuery = _context.Expenses.Where(e => !e.IsArchived && e.Date >= start && e.Date < end).AsQueryable();

                    if (branchId.HasValue)
                    {
                        salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                        expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
                    }

                    monthlySales = await salesQuery.SumAsync(s => s.Amount);
                    monthlyExpenses = await expensesQuery.SumAsync(e => e.Amount);

                    var breakdown = await expensesQuery
                        .GroupBy(e => e.Category)
                        .Select(g => new { Category = g.Key, Amount = g.Sum(e => e.Amount) })
                        .ToListAsync();

                    monthlyCogs = breakdown.FirstOrDefault(b => b.Category == "COGS")?.Amount ?? 0;
                    monthlyRent = breakdown.FirstOrDefault(b => b.Category == "Rent")?.Amount ?? 0;
                    monthlySalaries = breakdown.FirstOrDefault(b => b.Category == "Salaries")?.Amount ?? 0;
                    monthlyUtilities = breakdown.FirstOrDefault(b => b.Category == "Utilities")?.Amount ?? 0;
                }

                data.Add(new {
                    month = d.ToString("MMM yyyy") + (i == 0 && !submissions.Any() ? " (LIVE)" : ""),
                    monthKey = $"{d.Year}-{d.Month:D2}",
                    sales = monthlySales,
                    revenue = monthlySales, // Alias for consistency
                    expenses = monthlyExpenses,
                    netProfit = monthlySales - monthlyExpenses,
                    cogs = monthlyCogs,
                    rent = monthlyRent,
                    salaries = monthlySalaries,
                    utilities = monthlyUtilities
                });
            }

            return data;
        }

        public async Task<List<object>> GetBranchPerformanceAsync(int? month = null, int? year = null)
        {
            var branches = await _context.Branches.Where(b => !b.IsArchived).ToListAsync();
            var results = new List<object>();

            foreach (var b in branches)
            {
                decimal totalRevenue = 0;
                decimal totalExpenses = 0;

                if (month.HasValue && year.HasValue)
                {
                    // Filtered Period: Check for submission first
                    var submission = await _context.BranchSubmissions
                        .FirstOrDefaultAsync(s => s.BranchId == b.Id && s.SubmissionYear == year.Value && s.SubmissionMonth == month.Value);

                    if (submission != null)
                    {
                        totalRevenue = submission.SalesRevenue;
                        totalExpenses = submission.Expenses;
                    }
                    else
                    {
                        // Use raw transactional data for this specific month
                        var start = new DateTime(year.Value, month.Value, 1);
                        var end = start.AddMonths(1);

                        totalRevenue = await _context.Sales
                            .Where(s => s.BranchId == b.Id && !s.IsArchived && s.Date >= start && s.Date < end)
                            .SumAsync(s => s.Amount);
                        totalExpenses = await _context.Expenses
                            .Where(s => s.BranchId == b.Id && !s.IsArchived && s.Date >= start && s.Date < end)
                            .SumAsync(s => s.Amount);
                    }
                }
                else
                {
                    // ALL-TIME HYBRID PERFORMANCE
                    var submissionTotals = await _context.BranchSubmissions
                        .Where(s => s.BranchId == b.Id)
                        .Select(s => new { s.SalesRevenue, s.Expenses, s.SubmissionYear, s.SubmissionMonth })
                        .ToListAsync();

                    totalRevenue = submissionTotals.Sum(s => s.SalesRevenue);
                    totalExpenses = submissionTotals.Sum(s => s.Expenses);

                    var allLiveSales = await _context.Sales.Where(s => s.BranchId == b.Id && !s.IsArchived).ToListAsync();
                    var allLiveExpenses = await _context.Expenses.Where(e => e.BranchId == b.Id && !e.IsArchived).ToListAsync();

                    foreach (var s in allLiveSales)
                    {
                        if (!submissionTotals.Any(sub => sub.SubmissionYear == s.Date.Year && sub.SubmissionMonth == s.Date.Month))
                            totalRevenue += s.Amount;
                    }
                    foreach (var e in allLiveExpenses)
                    {
                        if (!submissionTotals.Any(sub => sub.SubmissionYear == e.Date.Year && sub.SubmissionMonth == e.Date.Month))
                            totalExpenses += e.Amount;
                    }
                }

                var profit = totalRevenue - totalExpenses;
                var margin = totalRevenue > 0 ? (double)(profit / totalRevenue) * 100 : 0;

                results.Add(new {
                    Id = b.Id,
                    Name = b.Name,
                    Revenue = (decimal)totalRevenue,
                    Expenses = (decimal)totalExpenses,
                    Profit = (decimal)profit,
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
