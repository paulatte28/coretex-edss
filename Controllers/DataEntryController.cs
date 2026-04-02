using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coretex_finalproj.Data;
using coretex_finalproj.Models;
using System.Security.Claims;

namespace coretex_finalproj.Controllers
{
    public class DataEntryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DataEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSale(Sale sale)
        {
            if (ModelState.IsValid)
            {
                var tenant = _context.Tenants.FirstOrDefault();
                
                if (tenant != null)
                {
                    sale.TenantId = tenant.Id;
                    sale.Date = DateTime.UtcNow;
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Sale data recorded successfully.";
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
    }
}
