using System;
using System.Collections.Generic;

namespace coretex_finalproj.Models
{
    public class DashboardViewModel
    {
        // KPI Summary Metrics
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int LowStockItems { get; set; }
        public decimal ProfitMargin { get; set; }
        public double RevenueGrowth { get; set; } // Percentage

        // Chart Data (Mock)
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<decimal> ExpenseData { get; set; } = new();

        // Recent Activity
        public List<TransactionViewModel> RecentTransactions { get; set; } = new();
    }

    public class TransactionViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // Completed, Pending, Shipped
        public DateTime Date { get; set; }
    }
}
