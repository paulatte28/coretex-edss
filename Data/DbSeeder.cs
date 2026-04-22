using coretex_finalproj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbSeeder");

            await EnsureSchemaAsync(context, logger);

            if (!await TableExistsAsync(context, "Branches"))
            {
                logger?.LogWarning("Skipping data seed because the Branches table is not present in the target database.");
                return;
            }

            Branch? defaultBranch = null;

            if (!await context.Branches.AnyAsync())
            {
                defaultBranch = new Branch
                {
                    Id = Guid.NewGuid(),
                    Name = "Main Branch",
                    Address = "Head Office",
                    BranchCode = "MAIN",
                    IsActive = true
                };
                context.Branches.Add(defaultBranch);
                await context.SaveChangesAsync();

                // Seed some initial products
                context.Products.AddRange(
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Premium Widget", Category = "Gadgets", Price = 199.99m, StockQuantity = 150, LowStockThreshold = 20 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Basic Component", Category = "Parts", Price = 15.50m, StockQuantity = 500, LowStockThreshold = 50 }
                );

                // Seed some initial sales
                context.Sales.AddRange(
                    new Sale { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, OrderId = "ORD-001", CustomerName = "Acme Corp", Amount = 1500.00m, Status = "Completed", Date = DateTime.UtcNow.AddDays(-2) },
                    new Sale { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, OrderId = "ORD-002", CustomerName = "Globex", Amount = 350.50m, Status = "Pending", Date = DateTime.UtcNow.AddDays(-1) }
                );

                // Seed some initial expenses
                context.Expenses.AddRange(
                    new Expense { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Category = "Software", Description = "Monthly SaaS", Amount = 300.00m, Date = DateTime.UtcNow.AddDays(-5) },
                    new Expense { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Category = "Utilities", Description = "Office Electricity", Amount = 150.00m, Date = DateTime.UtcNow.AddDays(-3) }
                );

                await context.SaveChangesAsync();
            }

            defaultBranch ??= await context.Branches
                .OrderByDescending(b => b.IsActive)
                .ThenBy(b => b.Name)
                .FirstAsync();

            await SeedRolesAndUsersAsync(userManager, roleManager, defaultBranch.Id, logger);
        }

        private static async Task EnsureSchemaAsync(ApplicationDbContext context, ILogger? logger)
        {
            logger?.LogInformation("Applying pending EF Core migrations before seeding.");
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning(
                    ex,
                    "Skipping automatic migration because model changes are pending without a new migration. Continuing with existing schema for seeding.");
            }
        }

        private static async Task SeedRolesAndUsersAsync(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Guid defaultBranchId,
            ILogger? logger)
        {
            var roles = new[] { "ADMIN", "CEO", "FINANCE", "CASHIER", "USER" };

            foreach (var role in roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!createRoleResult.Succeeded)
                {
                    logger?.LogWarning(
                        "Failed to create role {Role}: {Errors}",
                        role,
                        string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                }
            }

            await EnsureUserAsync(
                userManager,
                email: "admin@coretex.com",
                fullName: "System Administrator",
                password: "Admin123!",
                role: "ADMIN",
                branchId: null,
                logger: logger);

            await EnsureUserAsync(
                userManager,
                email: "ceo@coretex.com",
                fullName: "Chief Executive Officer",
                password: "Ceo12345!",
                role: "CEO",
                branchId: null,
                logger: logger);

            await EnsureUserAsync(
                userManager,
                email: "finance@coretex.com",
                fullName: "Finance Officer",
                password: "Finance123!",
                role: "FINANCE",
                branchId: defaultBranchId,
                logger: logger);

            await EnsureUserAsync(
                userManager,
                email: "cashier@coretex.com",
                fullName: "Branch Cashier",
                password: "Cashier123!",
                role: "CASHIER",
                branchId: defaultBranchId,
                logger: logger);
        }

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string email,
            string fullName,
            string password,
            string role,
            Guid? branchId,
            ILogger? logger)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    BranchId = branchId
                };

                var createUserResult = await userManager.CreateAsync(user, password);
                if (!createUserResult.Succeeded)
                {
                    logger?.LogWarning(
                        "Failed to create user {Email}: {Errors}",
                        email,
                        string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
                    return;
                }

                logger?.LogInformation("Created default user {Email}.", email);
            }
            else
            {
                var requiresUpdate = false;

                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = fullName;
                    requiresUpdate = true;
                }

                if (branchId.HasValue && user.BranchId != branchId)
                {
                    user.BranchId = branchId;
                    requiresUpdate = true;
                }

                if (requiresUpdate)
                {
                    var updateUserResult = await userManager.UpdateAsync(user);
                    if (!updateUserResult.Succeeded)
                    {
                        logger?.LogWarning(
                            "Failed to update user {Email}: {Errors}",
                            email,
                            string.Join(", ", updateUserResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    logger?.LogWarning(
                        "Failed to assign role {Role} to user {Email}: {Errors}",
                        role,
                        email,
                        string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        private static async Task<bool> TableExistsAsync(ApplicationDbContext context, string tableName)
        {
            var connection = context.Database.GetDbConnection();
            var openedHere = false;

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
                openedHere = true;
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT 1
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
            finally
            {
                if (openedHere)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
