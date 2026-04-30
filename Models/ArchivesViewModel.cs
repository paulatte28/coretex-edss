using System.Collections.Generic;

namespace coretex_finalproj.Models
{
    public class ArchivesViewModel
    {
        public List<Branch> ArchivedBranches { get; set; } = new List<Branch>();
        public List<Sale> ArchivedSales { get; set; } = new List<Sale>();
        public List<Expense> ArchivedExpenses { get; set; } = new List<Expense>();
    }
}
