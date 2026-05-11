using coretex_finalproj.Data;
using coretex_finalproj.Models;
using coretex_finalproj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Controllers
{
    [Authorize(Roles = "CASHIER,ADMIN")]
    public class CashierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLoggingService _auditLog;

        public CashierController(ApplicationDbContext context, AuditLoggingService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<IActionResult> Pos()
        {
            // EMERGENCY INJECTION: If no products exist, seed them now for the demo
            if (!await _context.Products.AnyAsync())
            {
                var branch = await _context.Branches.FirstOrDefaultAsync() ?? new Branch { Name = "Davao HQ", BranchCode = "DHQ-001" };
                if (branch.Id == Guid.Empty) {
                    _context.Branches.Add(branch);
                    await _context.SaveChangesAsync();
                }

                _context.Products.AddRange(
                    new Product { Name = "Coretex ZenBook Pro", Category = "Laptops", Price = 85000.00m, BranchId = branch.Id },
                    new Product { Name = "X-Series Workstation", Category = "Laptops", Price = 120000.00m, BranchId = branch.Id },
                    new Product { Name = "EliteBook Enterprise", Category = "Laptops", Price = 65000.00m, BranchId = branch.Id },
                    new Product { Name = "NVIDIA RTX 4090 (Core Edition)", Category = "Components", Price = 110000.00m, BranchId = branch.Id },
                    new Product { Name = "64GB DDR5 Server RAM", Category = "Components", Price = 18000.00m, BranchId = branch.Id },
                    new Product { Name = "2TB NVMe Gen5 SSD", Category = "Components", Price = 12500.00m, BranchId = branch.Id },
                    new Product { Name = "Rack-Mount Storage Node", Category = "Infrastructure", Price = 250000.00m, BranchId = branch.Id },
                    new Product { Name = "Enterprise Router AX9000", Category = "Infrastructure", Price = 45000.00m, BranchId = branch.Id },
                    new Product { Name = "Coretex Firewall Hub", Category = "Infrastructure", Price = 32000.00m, BranchId = branch.Id },
                    new Product { Name = "Coretex Security Suite (1yr)", Category = "Software", Price = 5500.00m, BranchId = branch.Id },
                    new Product { Name = "Cloud Backup Subscription", Category = "Software", Price = 1200.00m, BranchId = branch.Id }
                );
                await _context.SaveChangesAsync();
            }

            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            var branchId = user?.BranchId;

            if (branchId != null)
            {
                var branch = await _context.Branches.FindAsync(branchId);
                ViewBag.BranchName = branch?.Name;
            }

            var productQuery = _context.Products.AsQueryable();
            var salesQuery = _context.Sales.Where(s => s.Date >= DateTime.Today).AsQueryable();

            if (branchId != null && !User.IsInRole("ADMIN"))
            {
                productQuery = productQuery.Where(p => p.BranchId == branchId);
                salesQuery = salesQuery.Where(s => s.BranchId == branchId);
            }

            ViewBag.Products = await productQuery.OrderBy(p => p.Name).ToListAsync();
            var sales = await salesQuery.ToListAsync();
            return View(sales);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBulkSale([FromBody] List<Sale> items)
        {
            if (items == null || items.Count == 0) return BadRequest("No items in cart.");

            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("User not assigned to a branch.");

            var branch = await _context.Branches.FindAsync(user.BranchId);
            var branchPrefix = branch?.BranchCode?.Replace("-", "") ?? "NODE";
            var count = await _context.Sales.Select(s => s.OrderId).Distinct().CountAsync() + 1;
            var orderId = $"CTX-{branchPrefix}-{DateTime.Now.Year}-{count:D4}";
            var timestamp = DateTime.Now;

            foreach (var sale in items)
            {
                // Inventory decrement logic
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == sale.ProductName && p.BranchId == user.BranchId);
                if (product != null && product.StockQuantity >= sale.Quantity)
                {
                    product.StockQuantity -= sale.Quantity;
                    _context.Products.Update(product);
                }

                sale.OrderId = orderId;
                sale.Date = timestamp;
                sale.BranchId = user.BranchId.Value;
                _context.Sales.Add(sale);
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActivityAsync("SALE_BULK_CREATE", $"Processed checkout {orderId} with {items.Count} items. Total: {items.Sum(i => i.Amount):C}", user.BranchId);

            return Json(new { success = true, orderId = orderId });
        }

        public IActionResult DailySummary()
        {
            return View();
        }

        public async Task<IActionResult> Transactions(string search)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            var query = _context.Sales.Include(s => s.Branch).AsQueryable();

            // SECURITY ISOLATION: Branch staff can only see their own branch transactions
            if (user?.BranchId != null && !User.IsInRole("ADMIN"))
            {
                query = query.Where(s => s.BranchId == user.BranchId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.OrderId.Contains(search) || s.CustomerName.Contains(search));
            }

            ViewBag.SearchTerm = search;
            var sales = await query.OrderByDescending(s => s.Date).ToListAsync();
            return View(sales);
        }
        [HttpGet]
        public async Task<IActionResult> GetDailySummary(DateTime date)
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var sales = await _context.Sales
                .Where(s => s.BranchId == user.BranchId && s.Date >= startDate && s.Date < endDate && !s.IsArchived)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            var totalSales = sales.Sum(s => s.Amount);
            var topProduct = sales.GroupBy(s => s.ProductName)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .FirstOrDefault();

            return Json(new
            {
                totalTransactions = sales.Count,
                totalSales,
                topProductName = topProduct?.Name ?? "-",
                topProductQty = topProduct?.Qty ?? 0,
                transactions = sales.Select(s => new {
                    productName = s.ProductName,
                    quantity = s.Quantity,
                    unitPrice = s.UnitPrice,
                    lineTotal = s.Amount,
                    timestamp = s.Date
                })
            });
        }

        public async Task<IActionResult> StockHealth()
        {
            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null || user.BranchId == null) return BadRequest("Unauthorized.");

            var products = await _context.Products
                .Where(p => p.BranchId == user.BranchId)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            // EDSS LOGIC: Identify critical items
            var criticalItems = products.Where(p => p.StockQuantity <= p.LowStockThreshold).ToList();
            var healthyItems = products.Where(p => p.StockQuantity > p.LowStockThreshold).ToList();

            ViewBag.CriticalItems = criticalItems;
            ViewBag.HealthyItems = healthyItems;
            
            await _auditLog.LogActivityAsync("INVENTORY_AUDIT", $"Cashier {user.Email} performed a stock health surveillance check.", user.BranchId);

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> RequestRestock(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var userName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            
            // LOG THE DECISION SUPPORT ACTION
            await _auditLog.LogActivityAsync("RESTOCK_REQUEST", $"CRITICAL: Cashier requested urgent replenishment for {product.Name} (Current Stock: {product.StockQuantity})", user?.BranchId);

            return Json(new { success = true, message = $"Restock request for {product.Name} has been transmitted to management." });
        }

        public async Task<IActionResult> ActivityLog()
        {
            var userName = User.Identity?.Name;
            var logs = await _context.ActivityLogs
                .Where(l => l.UserName == userName)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(logs);
        }
    }
}