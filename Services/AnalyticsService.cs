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
            
            // SIMULATED BASE: Only add baseline for Global View (null branchId)
            // For specific branches, we use 100% Real, LIVE data.
            decimal baseSales = branchId.HasValue ? 0 : 2850000m; 
            decimal baseExpenses = branchId.HasValue ? 0 : 1620000m;

            var totalSales = baseSales + realSales;
            var totalExpenses = baseExpenses + realExpenses;
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

            // 1. Generate 5 months of "Historical Baseline"
            var random = new Random(42); // Seed for consistent demo data
            for (int i = 5; i >= 1; i--)
            {
                var d = now.AddMonths(-i);
                data.Add(new {
                    month = d.ToString("MMM yyyy"),
                    sales = (decimal)random.Next(480000, 620000),
                    expenses = (decimal)random.Next(320000, 380000)
                });
            }

            // 2. Add the CURRENT MONTH (Live Data from SQL)
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var salesQuery = _context.Sales.Where(s => !s.IsArchived && s.Date >= startOfMonth).AsQueryable();
            var expensesQuery = _context.Expenses.Where(e => !e.IsArchived && e.Date >= startOfMonth).AsQueryable();

            if (branchId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
            }

            var currentSales = await salesQuery.SumAsync(s => s.Amount);
            var currentExpenses = await expensesQuery.SumAsync(e => e.Amount);

            data.Add(new {
                month = now.ToString("MMM yyyy") + " (LIVE)",
                sales = currentSales,
                expenses = currentExpenses
            });

            return data;
        }

        public async Task<object> GetBranchPerformanceAsync()
        {
            return await _context.Branches
                .Where(b => !b.IsArchived)
                .Select(b => new {
                    Name = b.Name,
                    TotalSales = _context.Sales.Where(s => s.BranchId == b.Id && !s.IsArchived).Sum(s => s.Amount),
                    TotalExpenses = _context.Expenses.Where(e => e.BranchId == b.Id && !e.IsArchived).Sum(e => e.Amount)
                })
                .ToListAsync();
        }

        public async Task<object> GetExpenseCategoriesAsync(Guid? branchId = null)
        {
            var query = _context.Expenses.Where(e => !e.IsArchived).AsQueryable();
            if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId.Value);

            return await query
                .GroupBy(e => e.Category)
                .Select(g => new { 
                    Category = g.Key, 
                    Amount = g.Sum(e => e.Amount) 
                })
                .ToListAsync();
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
