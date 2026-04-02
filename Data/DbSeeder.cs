using coretex_finalproj.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace coretex_finalproj.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure DB and schema are created
            await context.Database.EnsureCreatedAsync();

            if (!context.Tenants.Any())
            {
                var defaultTenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    CompanyName = "Coretex Demo Company",
                    PlanType = "Enterprise",
                    Status = "Active",
                    JoinedDate = DateTime.UtcNow.AddMonths(-1)
                };
                context.Tenants.Add(defaultTenant);
                await context.SaveChangesAsync();

                // Seed some initial products
                context.Products.AddRange(
                    new Product { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, Name = "Premium Widget", Category = "Gadgets", Price = 199.99m, StockQuantity = 150, LowStockThreshold = 20 },
                    new Product { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, Name = "Basic Component", Category = "Parts", Price = 15.50m, StockQuantity = 500, LowStockThreshold = 50 }
                );

                // Seed some initial sales
                context.Sales.AddRange(
                    new Sale { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, OrderId = "ORD-001", CustomerName = "Acme Corp", Amount = 1500.00m, Status = "Completed", Date = DateTime.UtcNow.AddDays(-2) },
                    new Sale { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, OrderId = "ORD-002", CustomerName = "Globex", Amount = 350.50m, Status = "Pending", Date = DateTime.UtcNow.AddDays(-1) }
                );

                // Seed some initial expenses
                context.Expenses.AddRange(
                    new Expense { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, Category = "Software", Description = "Monthly SaaS", Amount = 300.00m, Date = DateTime.UtcNow.AddDays(-5) },
                    new Expense { Id = Guid.NewGuid(), TenantId = defaultTenant.Id, Category = "Utilities", Description = "Office Electricity", Amount = 150.00m, Date = DateTime.UtcNow.AddDays(-3) }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
