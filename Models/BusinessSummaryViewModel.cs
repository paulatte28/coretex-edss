using System;

namespace coretex_finalproj.Models
{
    public class BusinessSummaryViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public int ActiveBranches { get; set; }
        public ReportSchedule? CurrentSchedule { get; set; }
        public string TopPerformingBranch { get; set; } = "N/A";
    }
}
