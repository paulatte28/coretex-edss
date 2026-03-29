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
        public List<string> MonthlyLabels { get; set; }
        public List<decimal> RevenueData { get; set; }
        public List<decimal> ExpenseData { get; set; }

        // Recent Activity
        public List<TransactionViewModel> RecentTransactions { get; set; }
    }

    public class TransactionViewModel
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } // Completed, Pending, Shipped
        public DateTime Date { get; set; }
    }
}
