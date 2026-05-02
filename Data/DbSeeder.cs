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

            // PROACTIVE PATCH: Rename "Main Branch" to "Claveria HQ" if it exists
            var existingMainBranch = await context.Branches.FirstOrDefaultAsync(b => b.Name == "Main Branch");
            if (existingMainBranch != null)
            {
                existingMainBranch.Name = "Claveria HQ";
                existingMainBranch.Address = "Claveria St., Davao City";
                existingMainBranch.BranchCode = "CHQ-001";
                await context.SaveChangesAsync();
                logger?.LogInformation("Migrated 'Main Branch' to 'Claveria HQ'.");
            }

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
                    Name = "Claveria HQ",
                    Address = "Claveria St., Davao City",
                    BranchCode = "CHQ-001",
                    IsActive = true
                };
                context.Branches.Add(defaultBranch);

                // Add Other Specific Branches
                context.Branches.AddRange(
                    new Branch { Name = "Sandawa Branch", Address = "Sandawa, Matina", BranchCode = "SND-002" },
                    new Branch { Name = "Toril Branch", Address = "Toril District", BranchCode = "TRL-003" },
                    new Branch { Name = "Agdao Branch", Address = "Agdao Public Market", BranchCode = "AGD-004" },
                    new Branch { Name = "Buhangin Branch", Address = "Buhangin Highway", BranchCode = "BHG-005" }
                );

                await context.SaveChangesAsync();

                // Seed some initial products
                context.Products.AddRange(
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Premium Widget", Category = "Gadgets", Price = 199.99m, StockQuantity = 150, LowStockThreshold = 20 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Basic Component", Category = "Parts", Price = 15.50m, StockQuantity = 500, LowStockThreshold = 50 }
                );

                // Seed some initial sales
                context.Sales.AddRange(
                    new Sale { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, OrderId = "ORD-001", CustomerName = "Acme Corp", Amount = 1500.00m, ProductName = "Premium Widget", Quantity = 5, UnitPrice = 300.00m, Status = "Completed", Date = DateTime.UtcNow.AddDays(-2) },
                    new Sale { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, OrderId = "ORD-002", CustomerName = "Globex", Amount = 350.50m, ProductName = "Basic Component", Quantity = 10, UnitPrice = 35.05m, Status = "Pending", Date = DateTime.UtcNow.AddDays(-1) }
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

            await SeedRolesAndUsersAsync(context, userManager, roleManager, defaultBranch.Id, logger);
        }

        private static async Task EnsureSchemaAsync(ApplicationDbContext context, ILogger? logger)
        {
            logger?.LogInformation("Checking database schema...");
            try
            {
                // If there are no local migrations, just ensure the DB is created
                await context.Database.EnsureCreatedAsync();
                logger?.LogInformation("Database schema is ready.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning("Database check completed with a notice: {Message}. Continuing to seed data anyway.", ex.Message);
            }
        }

        private static async Task SeedRolesAndUsersAsync(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Guid defaultBranchId,
            ILogger? logger)
        {
            var roles = new[] { "ADMIN", "CEO", "FINANCE", "CASHIER", "BRANCH_ADMIN", "USER" };

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
                password: "Password12345!",
                role: "ADMIN",
                branchId: null,
                logger: logger);

            await EnsureUserAsync(
                userManager,
                email: "ceo@coretex.com",
                fullName: "Chief Executive Officer",
                password: "Password12345!",
                role: "CEO",
                branchId: null,
                logger: logger);

            // --- Multi-Branch Staffing Loop ---
            var allBranches = await context.Branches.ToListAsync();
            foreach (var branch in allBranches)
            {
                var branchSlug = branch.Name.ToLower().Replace(" ", "");
                
                // 1. Branch Manager
                await EnsureUserAsync(
                    userManager,
                    email: $"{branchSlug}_manager@coretex.com",
                    fullName: $"{branch.Name} Manager",
                    password: "Password12345!",
                    role: "BRANCH_ADMIN",
                    branchId: branch.Id,
                    logger: logger);

                // 2. Finance Officer
                await EnsureUserAsync(
                    userManager,
                    email: $"{branchSlug}_finance@coretex.com",
                    fullName: $"{branch.Name} Finance",
                    password: "Password12345!",
                    role: "FINANCE",
                    branchId: branch.Id,
                    logger: logger);

                // 3. Branch Cashier
                await EnsureUserAsync(
                    userManager,
                    email: $"{branchSlug}_cashier@coretex.com",
                    fullName: $"{branch.Name} Cashier",
                    password: "Password12345!",
                    role: "CASHIER",
                    branchId: branch.Id,
                    logger: logger);
            }

            // Keep original credentials working for backward compatibility
            var defaultOpsBranchId = allBranches.FirstOrDefault(b => b.Name.Contains("Sandawa"))?.Id ?? defaultBranchId;
            await EnsureUserAsync(userManager, "manager@coretex.com", "Default Manager", "Password12345!", "BRANCH_ADMIN", defaultOpsBranchId, logger);
            await EnsureUserAsync(userManager, "finance@coretex.com", "Default Finance", "Password12345!", "FINANCE", defaultOpsBranchId, logger);
            await EnsureUserAsync(userManager, "cashier@coretex.com", "Default Cashier", "Password12345!", "CASHIER", defaultOpsBranchId, logger);
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
                    TwoFactorEnabled = true,
                    FullName = fullName,
                    BranchId = branchId,
                    Role = role
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

                if (!user.TwoFactorEnabled)
                {
                    user.TwoFactorEnabled = true;
                    requiresUpdate = true;
                }

                if (user.Role != role)
                {
                    user.Role = role;
                    requiresUpdate = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
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

                // --- NIST Compliance: Force Update Password for Default Accounts ---
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, resetToken, password);
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
