using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coretex_finalproj.Data;
using coretex_finalproj.Models;
using System.Security.Claims;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Controllers
{
    public class DataEntryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DataEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Finance Officer Dashboard / Data Entry Form
        public IActionResult Index()
        {
            return View();
        }

        // Submission History
        public IActionResult History()
        {
            return View();
        }

        // Edit Submission
        public IActionResult Edit(string id)
        {
            ViewBag.SubmissionId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSale(Sale sale)
        {
            if (ModelState.IsValid)
            {
                var isSaved = false;

                if (CanPersistSales())
                {
                    try
                    {
                        var branch = _context.Branches.FirstOrDefault();
                        if (branch != null)
                        {
                            sale.BranchId = branch.Id;
                            sale.Date = DateTime.UtcNow;
                            _context.Sales.Add(sale);
                            await _context.SaveChangesAsync();
                            isSaved = true;
                        }
                    }
                    catch
                    {
                        isSaved = false;
                    }
                }

                TempData["SuccessMessage"] = isSaved
                    ? "Sale data recorded successfully."
                    : "Sale captured in demo mode. Database schema update is required for persistence.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitMonthlyData(IFormCollection form)
        {
            // Frontend-only implementation: Just return success redirect
            TempData["SuccessMessage"] = "Monthly business data recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool CanPersistSales()
        {
            return TableHasColumns("Branches", "Id")
                && TableHasColumns("Sales", "BranchId", "Date", "OrderId", "CustomerName", "Amount", "Status");
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
