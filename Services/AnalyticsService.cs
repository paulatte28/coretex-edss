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

            var sales = await salesQuery.SumAsync(s => s.Amount);
            var expenses = await expensesQuery.SumAsync(e => e.Amount);
            var profit = sales - expenses;
            var margin = sales > 0 ? (profit / sales) * 100 : 0;

            // Fetch dynamic threshold from DB
            var threshold = await _context.KpiThresholds.FirstOrDefaultAsync(t => t.IsActive);
            var minMargin = threshold?.MinProfitMargin ?? 15m;
            
            var riskLevel = "Healthy";
            if (margin < minMargin) riskLevel = "Warning";
            if (profit < 0) riskLevel = "Critical";

            return new DashboardSnapshot
            {
                TotalRevenue = sales,
                TotalExpenses = expenses,
                NetProfit = profit,
                ProfitMargin = margin,
                RiskLevel = riskLevel
            };
        }

        public async Task<object> GetMonthlyProfitLossAsync(Guid? branchId = null)
        {
            var salesQuery = _context.Sales.Where(s => !s.IsArchived).AsQueryable();
            var expensesQuery = _context.Expenses.Where(e => !e.IsArchived).AsQueryable();

            if (branchId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
            }

            var rawData = await salesQuery
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .Select(g => new { 
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Sales = g.Sum(s => s.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var data = rawData.Select(x => new {
                Month = $"{x.Year}-{x.Month:D2}",
                Sales = x.Sales,
                Expenses = expensesQuery
                        .Where(e => e.Date.Year == x.Year && e.Date.Month == x.Month)
                        .Sum(e => e.Amount)
            }).ToList();

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
