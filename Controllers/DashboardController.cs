using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using coretex_finalproj.Models;

namespace coretex_finalproj.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Initializing mock data for the Executive Dashboard
            var model = new DashboardViewModel
            {
                TotalRevenue = 125430.50m,
                TotalOrders = 452,
                LowStockItems = 12,
                ProfitMargin = 24.5m,
                RevenueGrowth = 8.4,
                MonthlyLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                RevenueData = new List<decimal> { 12000, 15000, 18000, 16000, 21000, 25000 },
                ExpenseData = new List<decimal> { 9000, 11000, 13000, 12500, 14000, 15500 },
                RecentTransactions = new List<TransactionViewModel>
                {
                    new TransactionViewModel { OrderId = "INV-001", CustomerName = "John Doe", Amount = 1200.00m, Status = "Completed", Date = DateTime.Now.AddDays(-1) },
                    new TransactionViewModel { OrderId = "INV-002", CustomerName = "Jane Smith", Amount = 850.50m, Status = "Pending", Date = DateTime.Now.AddDays(-2) },
                    new TransactionViewModel { OrderId = "INV-003", CustomerName = "Robert Brown", Amount = 2400.00m, Status = "Shipped", Date = DateTime.Now.AddDays(-3) },
                    new TransactionViewModel { OrderId = "INV-004", CustomerName = "Michael Wilson", Amount = 350.25m, Status = "Completed", Date = DateTime.Now.AddDays(-4) },
                    new TransactionViewModel { OrderId = "INV-005", CustomerName = "Emily Davis", Amount = 1540.00m, Status = "Completed", Date = DateTime.Now.AddDays(-5) }
                }
            };

            return View(model);
        }
    }
}
