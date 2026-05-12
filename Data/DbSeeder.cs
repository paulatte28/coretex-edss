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

            // 1. PURGE ALL USERS EXCEPT MASTER ADMINS (The first thing we do)
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var u in allUsers)
            {
                if (u.Email != "ceo@coretex.com" && u.Email != "admin@coretex.com")
                {
                    await userManager.DeleteAsync(u);
                }
            }

            // 2. SEARCH & DESTROY LEGACY BRANCHES (Resolves FK conflicts by deleting users first)
            var oldBranches = await context.Branches
                .Where(b => b.Name.Contains("Claveria") || b.Name == "Sandawa Branch" || b.BranchCode == "SND-002")
                .ToListAsync();
            
            if (oldBranches.Any())
            {
                context.Branches.RemoveRange(oldBranches);
                await context.SaveChangesAsync();
            }

            // 3. ENTERPRISE SCALE SEEDING (5 BRANCHES)
            var requiredBranches = new[]
            {
                new { Name = "Sandawa", Address = "Sandawa, Matina", Code = "CORETEX-SANDAWA" },
                new { Name = "Mintal", Address = "Mintal, Davao City", Code = "CORETEX-MINTAL" },
                new { Name = "Toril", Address = "Toril, Davao City", Code = "CORETEX-TORIL" },
                new { Name = "Buhangin", Address = "Buhangin, Davao City", Code = "CORETEX-BUHANGIN" },
                new { Name = "Matina", Address = "Matina Crossing, Davao City", Code = "CORETEX-MATINA" }
            };

            foreach (var rb in requiredBranches)
            {
                var existingBranch = await context.Branches.FirstOrDefaultAsync(b => b.Name == rb.Name || b.BranchCode == rb.Code || b.Name == rb.Name + " Branch");
                
                if (existingBranch == null)
                {
                    context.Branches.Add(new Branch 
                    { 
                        Name = rb.Name, 
                        Address = rb.Address, 
                        BranchCode = rb.Code,
                        IsActive = true 
                    });
                }
                else
                {
                    // FORCE UPDATE: Ensure existing branches match the new naming standard
                    existingBranch.Name = rb.Name;
                    existingBranch.BranchCode = rb.Code;
                    existingBranch.IsArchived = false;
                    existingBranch.IsActive = true;
                    context.Branches.Update(existingBranch);
                }
            }
            await context.SaveChangesAsync();

            var allBranches = await context.Branches.ToListAsync();
            
            // Ensure every branch has a base inventory for the presentation
            foreach (var branch in allBranches)
            {
                if (!await context.Products.AnyAsync(p => p.BranchId == branch.Id))
                {
                    logger?.LogInformation($"Replenishing inventory for node: {branch.Name}...");
                    context.Products.AddRange(
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Coretex ZenBook Pro", Category = "Laptops", Price = 85000.00m, StockQuantity = 25, LowStockThreshold = 5 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "X-Series Workstation", Category = "Laptops", Price = 120000.00m, StockQuantity = 10, LowStockThreshold = 2 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "EliteBook Enterprise", Category = "Laptops", Price = 65000.00m, StockQuantity = 40, LowStockThreshold = 10 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "NVIDIA RTX 4090 (Core Edition)", Category = "Components", Price = 110000.00m, StockQuantity = 15, LowStockThreshold = 3 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "64GB DDR5 Server RAM", Category = "Components", Price = 18000.00m, StockQuantity = 100, LowStockThreshold = 20 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "2TB NVMe Gen5 SSD", Category = "Components", Price = 12500.00m, StockQuantity = 200, LowStockThreshold = 30 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Rack-Mount Storage Node", Category = "Infrastructure", Price = 250000.00m, StockQuantity = 5, LowStockThreshold = 1 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Enterprise Router AX9000", Category = "Infrastructure", Price = 45000.00m, StockQuantity = 12, LowStockThreshold = 2 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Coretex Firewall Hub", Category = "Infrastructure", Price = 32000.00m, StockQuantity = 8, LowStockThreshold = 2 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Coretex Security Suite (1yr)", Category = "Software", Price = 5500.00m, StockQuantity = 1000, LowStockThreshold = 100 },
                        new Product { Id = Guid.NewGuid(), BranchId = branch.Id, Name = "Cloud Backup Subscription", Category = "Software", Price = 1200.00m, StockQuantity = 1000, LowStockThreshold = 100 }
                    );
                }
            }
            await context.SaveChangesAsync();

            var firstBranch = allBranches.First();
            await SeedRolesAndUsersAsync(context, userManager, roleManager, firstBranch.Id, logger);
            await SeedTransactionsAsync(context, logger);
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
                password: "PasswordAdmin1234!",
                role: "ADMIN",
                branchId: null,
                logger: logger);

            await EnsureUserAsync(
                userManager,
                email: "ceo@coretex.com",
                fullName: "Chief Executive Officer",
                password: "PasswordCeo1234!",
                role: "CEO",
                branchId: null,
                logger: logger);

            // --- Multi-Branch Staffing Loop (Professional Naming Convention) ---
            var allBranches = await context.Branches.ToListAsync();
            foreach (var branch in allBranches)
            {
                // Clean name for email (e.g., "Sandawa Branch" -> "sandawa")
                var branchClean = branch.Name.ToLower().Replace(" branch", "").Replace(" ", "");
                // Camel case for password (e.g., "Sandawa" -> "Sandawa")
                var branchPascal = char.ToUpper(branchClean[0]) + branchClean.Substring(1);
                
                // --- NIST COMPLIANCE: Ensure Passwords are 12+ Characters ---
                string passSuffix = "2026!@#"; 
                // 1. Branch Manager
                await EnsureUserAsync(
                    userManager,
                    email: $"manager.{branchClean}@coretex.com",
                    fullName: $"{branch.Name} Manager",
                    password: $"Manager{branchPascal}{passSuffix}",
                    role: "BRANCH_ADMIN",
                    branchId: branch.Id,
                    logger: logger);

                // 2. Finance Officer
                await EnsureUserAsync(
                    userManager,
                    email: $"finance.{branchClean}@coretex.com",
                    fullName: $"{branch.Name} Finance",
                    password: $"Finance{branchPascal}{passSuffix}",
                    role: "FINANCE",
                    branchId: branch.Id,
                    logger: logger);

                // 3. 5 Branch Cashiers (Presentation Requirement)
                for (int i = 1; i <= 5; i++)
                {
                    await EnsureUserAsync(
                        userManager,
                        email: $"cashier{i}.{branchClean}@coretex.com",
                        fullName: $"{branch.Name} Cashier {i}",
                        password: $"Cashier{branchPascal}{i}{passSuffix}",
                        role: "CASHIER",
                        branchId: branch.Id,
                        logger: logger);
                }
            }
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
        private static async Task SeedTransactionsAsync(ApplicationDbContext context, ILogger? logger)
        {
            var now = DateTime.Now;
            logger?.LogInformation("SURGICAL SYNC: Purging future records and synchronizing Year-to-Date data...");
            
            // 1. SURGICAL PURGE: Only remove "Future Data" (past today)
            var futureSales = context.Sales.Where(s => s.Date > now).ToList();
            var futureExpenses = context.Expenses.Where(e => e.Date > now).ToList();
            
            context.Sales.RemoveRange(futureSales);
            context.Expenses.RemoveRange(futureExpenses);
            await context.SaveChangesAsync();

            var branches = await context.Branches.ToListAsync();
            var random = new Random();

            foreach (var branch in branches)
            {
                logger?.LogInformation($"Synchronizing tactical data for branch: {branch.Name}...");

                // 2. SEED SALES (Only for empty months)
                for (int m = 1; m <= now.Month; m++) 
                {
                    // Check if this month already has data (user created or previously seeded)
                    if (await context.Sales.AnyAsync(s => s.BranchId == branch.Id && s.Date.Month == m && s.Date.Year == now.Year))
                    {
                        continue; // Skip seeding if data already exists
                    }

                    int maxDay = (m == now.Month) ? now.Day : 28;
                    for (int i = 0; i < 25; i++) 
                    {
                        var day = random.Next(1, maxDay + 1);
                        int maxHour = (m == now.Month && day == now.Day) ? now.Hour : 20;
                        int maxMinute = (m == now.Month && day == now.Day && maxHour == now.Hour) ? now.Minute : 59;
                        
                        var saleDate = new DateTime(now.Year, m, day, random.Next(8, Math.Max(9, maxHour + 1)), random.Next(0, Math.Max(1, maxMinute)), 0);
                        var sale = new Sale
                        {
                            Id = Guid.NewGuid(),
                            BranchId = branch.Id,
                            Amount = (decimal)(random.NextDouble() * 12000 + 3000),
                            Date = saleDate,
                            CustomerName = i % 3 == 0 ? "Global Solutions Inc" : "Local Retail Partner",
                            OrderId = $"CTX-{branch.Name.Substring(0,3).ToUpper()}-{m:D2}{i:D2}-{random.Next(100, 999)}",
                            ProductName = i % 2 == 0 ? "Coretex ZenBook Pro" : "NVIDIA RTX 4090 (Core Edition)",
                            Quantity = random.Next(1, 3),
                            UnitPrice = 0 
                        };
                        context.Sales.Add(sale);
                    }
                }

                // 3. Seed Expenses (Only for empty months)
                var coreCategories = new[] { "COGS", "Rent", "Salaries", "Utilities", "Supplies", "Marketing" };
                for (int m = 1; m <= now.Month; m++)
                {
                    if (await context.Expenses.AnyAsync(e => e.BranchId == branch.Id && e.Date.Month == m && e.Date.Year == now.Year))
                    {
                        continue;
                    }

                    int maxDay = (m == now.Month) ? now.Day : 28;
                    for (int i = 0; i < 15; i++)
                    {
                        var cat = coreCategories[i % coreCategories.Length];
                        int maxHour = (m == now.Month && maxDay == now.Day) ? now.Hour : 20;
                        int maxMinute = (m == now.Month && maxDay == now.Day && maxHour == now.Hour) ? now.Minute : 59;

                        var expenseDate = new DateTime(now.Year, m, random.Next(1, maxDay + 1), random.Next(8, Math.Max(9, maxHour + 1)), random.Next(0, Math.Max(1, maxMinute)), 0);
                        var expense = new Expense
                        {
                            Id = Guid.NewGuid(),
                            BranchId = branch.Id,
                            Amount = cat == "COGS" ? (decimal)(random.NextDouble() * 8000 + 5000) : (decimal)(random.NextDouble() * 3000 + 1000),
                            Date = expenseDate,
                            Category = cat,
                            Description = $"{cat} - Operational Expenditure"
                        };
                        context.Expenses.Add(expense);
                    }
                }

                // 4. Seed Submissions (Historical Jan-April)
                for (int m = 1; m < now.Month; m++)
                {
                    if (await context.BranchSubmissions.AnyAsync(s => s.BranchId == branch.Id && s.SubmissionMonth == m && s.SubmissionYear == now.Year))
                        continue;

                    var variance = (decimal)(random.NextDouble() * 0.4 + 0.8);
                    var salesTotal = 250000m * variance;
                    context.BranchSubmissions.Add(new BranchSubmission
                    {
                        Id = Guid.NewGuid(),
                        BranchId = branch.Id,
                        SubmissionYear = now.Year,
                        SubmissionMonth = m,
                        SalesRevenue = salesTotal,
                        Expenses = 120000m * (decimal)(random.NextDouble() * 0.2 + 0.9),
                        Cogs = salesTotal * 0.45m,
                        Rent = 25000,
                        Salaries = 45000,
                        Utilities = 8000,
                        Status = "Approved",
                        SubmittedAt = new DateTime(now.Year, m, random.Next(1, 5)),
                        Notes = "Historical synchronization completed."
                    });
                }

                // 5. Seed Goals (Only missing months)
                for (int m = 1; m <= now.Month; m++)
                {
                    if (await context.BranchGoals.AnyAsync(bg => bg.BranchId == branch.Id && bg.Month == m && bg.Year == now.Year))
                        continue;

                    context.BranchGoals.Add(new BranchGoal
                    {
                        Id = Guid.NewGuid(),
                        BranchId = branch.Id,
                        TargetRevenue = 300000m,
                        Month = m,
                        Year = now.Year
                    });
                }
            }
            await context.SaveChangesAsync();
            logger?.LogInformation("Transaction and Goal seeding completed successfully.");
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
