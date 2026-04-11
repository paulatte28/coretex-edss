using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using coretex_finalproj.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using coretex_finalproj.Data;
using System.Linq;
using coretex_finalproj.Services;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CurrencyService _currencyService;

        public DashboardController(ApplicationDbContext context, CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public IActionResult Index()
        {
            Guid? branchId = null;
            var sales = new List<Sale>();
            var expenses = new List<Expense>();
            var products = new List<Product>();

            if (CanUseBusinessSchema())
            {
                try
                {
                    branchId = _context.Branches.Select(b => (Guid?)b.Id).FirstOrDefault();

                    if (branchId.HasValue)
                    {
                        sales = _context.Sales.Where(s => s.BranchId == branchId.Value).ToList();
                        expenses = _context.Expenses.Where(e => e.BranchId == branchId.Value).ToList();
                        products = _context.Products.Where(p => p.BranchId == branchId.Value).ToList();
                    }
                    else
                    {
                        sales = _context.Sales.ToList();
                        expenses = _context.Expenses.ToList();
                        products = _context.Products.ToList();
                    }
                }
                catch
                {
                    branchId = null;
                    sales = new List<Sale>();
                    expenses = new List<Expense>();
                    products = new List<Product>();
                }
            }

            decimal totalRevenue = sales.Sum(s => s.Amount);
            decimal totalExpenses = expenses.Sum(e => e.Amount);
            decimal profitMargin = totalRevenue > 0 ? ((totalRevenue - totalExpenses) / totalRevenue) * 100 : 0;
            
            var model = new DashboardViewModel
            {
                TotalRevenue = totalRevenue > 0 ? totalRevenue : 125430.50m, // Mock fallback for UI visually
                TotalOrders = sales.Count > 0 ? sales.Count : 452,
                LowStockItems = products.Count > 0 ? products.Count(p => p.StockQuantity < p.LowStockThreshold) : 12,
                ProfitMargin = totalRevenue > 0 ? Math.Round(profitMargin, 2) : 24.5m,
                RevenueGrowth = 8.4,
                MonthlyLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                RevenueData = new List<decimal> { 12000, 15000, 18000, 16000, 21000, totalRevenue > 0 ? totalRevenue : 25000 },
                ExpenseData = new List<decimal> { 9000, 11000, 13000, 12500, 14000, totalExpenses > 0 ? totalExpenses : 15500 },
                RecentTransactions = sales.Count > 0 
                    ? sales.OrderByDescending(s => s.Date).Take(5).Select(s => new TransactionViewModel
                        { OrderId = s.OrderId, CustomerName = s.CustomerName, Amount = s.Amount, Status = s.Status, Date = s.Date }).ToList()
                    : new List<TransactionViewModel> 
                    {
                        new TransactionViewModel { OrderId = "INV-001", CustomerName = "John Doe", Amount = 1200.00m, Status = "Completed", Date = DateTime.Now.AddDays(-1) }
                    }
            };

            return View(model);
        }

        public IActionResult TrendAnalysis()
        {
            return View();
        }

        public IActionResult ExecutiveReporting()
        {
            return View();
        }

        public IActionResult DataVisualization()
        {
            return View();
        }

        public IActionResult DecisionSupport()
        {
            return View();
        }

        public IActionResult BranchManagement()
        {
            List<Branch> branches = new List<Branch>();
            if (TableHasColumns("Branches", "Id", "Name", "Address", "BranchCode", "IsActive"))
            {
                try
                {
                    branches = _context.Branches.ToList();
                }
                catch
                {
                    branches = new List<Branch>();
                }
            }

            if (!branches.Any())
            {
                // Provide some mock branches for the frontend
                branches = new List<Branch>
                {
                    new Branch { Name = "Davao Branch", Address = "Davao City", BranchCode = "DAV", IsActive = true },
                    new Branch { Name = "Tagum Branch", Address = "Tagum City", BranchCode = "TAG", IsActive = true },
                    new Branch { Name = "Digos Branch", Address = "Digos City", BranchCode = "DIG", IsActive = false }
                };
            }
            return View("~/Views/Admin/BranchManagement.cshtml", branches);
        }

        public IActionResult Subscription()
        {
            return View();
        }

        private bool CanUseBusinessSchema()
        {
            return TableHasColumns("Branches", "Id", "Name", "Address", "BranchCode", "IsActive")
                && TableHasColumns("Sales", "Id", "BranchId", "Amount", "Status", "Date", "OrderId", "CustomerName")
                && TableHasColumns("Expenses", "Id", "BranchId", "Amount", "Date", "Category", "Description")
                && TableHasColumns("Products", "Id", "BranchId", "StockQuantity", "LowStockThreshold");
        }

        private bool TableHasColumns(string tableName, params string[] columns)
        {
            foreach (var column in columns)
            {
                if (!ColumnExists(tableName, column))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            var connection = _context.Database.GetDbConnection();
            var openedHere = false;

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                openedHere = true;
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = @tableName
  AND COLUMN_NAME = @columnName";

                var tableParameter = command.CreateParameter();
                tableParameter.ParameterName = "@tableName";
                tableParameter.Value = tableName;
                command.Parameters.Add(tableParameter);

                var columnParameter = command.CreateParameter();
                columnParameter.ParameterName = "@columnName";
                columnParameter.Value = columnName;
                command.Parameters.Add(columnParameter);

                return command.ExecuteScalar() != null;
            }
            finally
            {
                if (openedHere)
                {
                    connection.Close();
                }
            }
        }
    }
}
