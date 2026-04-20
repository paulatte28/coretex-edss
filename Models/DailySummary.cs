using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class DailySummary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime Date { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }

        public int TransactionCount { get; set; }
        
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
