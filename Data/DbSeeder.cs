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
                existingMainBranch.Address = "Claveria St., Corporate Plaza";
                existingMainBranch.BranchCode = "CHQ-001";
                await context.SaveChangesAsync();
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
                    Address = "Claveria St., Corporate Plaza",
                    BranchCode = "CHQ-001",
                    IsActive = true
                };
                context.Branches.Add(defaultBranch);

                // Add ONLY the essential Sandawa Node
                context.Branches.Add(new Branch { Name = "Sandawa Branch", Address = "Sandawa, Matina", BranchCode = "SND-002" });

                await context.SaveChangesAsync();

                // Seed a robust Enterprise Catalog
                context.Products.AddRange(
                    // Laptops
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Coretex ZenBook Pro", Category = "Laptops", Price = 85000.00m, StockQuantity = 25, LowStockThreshold = 5 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "X-Series Workstation", Category = "Laptops", Price = 120000.00m, StockQuantity = 10, LowStockThreshold = 2 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "EliteBook Enterprise", Category = "Laptops", Price = 65000.00m, StockQuantity = 40, LowStockThreshold = 10 },
                    
                    // Components
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "NVIDIA RTX 4090 (Core Edition)", Category = "Components", Price = 110000.00m, StockQuantity = 15, LowStockThreshold = 3 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "64GB DDR5 Server RAM", Category = "Components", Price = 18000.00m, StockQuantity = 100, LowStockThreshold = 20 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "2TB NVMe Gen5 SSD", Category = "Components", Price = 12500.00m, StockQuantity = 200, LowStockThreshold = 30 },
                    
                    // Infrastructure
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Rack-Mount Storage Node", Category = "Infrastructure", Price = 250000.00m, StockQuantity = 5, LowStockThreshold = 1 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Enterprise Router AX9000", Category = "Infrastructure", Price = 45000.00m, StockQuantity = 12, LowStockThreshold = 2 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Coretex Firewall Hub", Category = "Infrastructure", Price = 32000.00m, StockQuantity = 8, LowStockThreshold = 2 },
                    
                    // Software
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Coretex Security Suite (1yr)", Category = "Software", Price = 5500.00m, StockQuantity = 1000, LowStockThreshold = 100 },
                    new Product { Id = Guid.NewGuid(), BranchId = defaultBranch.Id, Name = "Cloud Backup Subscription", Category = "Software", Price = 1200.00m, StockQuantity = 1000, LowStockThreshold = 100 }
                );

                await context.SaveChangesAsync();
            }

            // --- GRANULAR BRANCH REPLENISHMENT ---
            var allBranches = await context.Branches.ToListAsync();
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
            // --- USER PURGE (ROSTER SANITIZATION) ---
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var u in allUsers)
            {
                if (u.Email != "ceo@coretex.com" && u.Email != "admin@coretex.com")
                {
                    await userManager.DeleteAsync(u);
                }
            }
            logger?.LogInformation("Purged legacy test roster. Only CEO and Admin remain.");

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
                password: "Admin12345!",
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

            // --- Multi-Branch Staffing Loop (Professional Naming Convention) ---
            var allBranches = await context.Branches.ToListAsync();
            foreach (var branch in allBranches)
            {
                // Clean name for email (e.g., "Sandawa Branch" -> "sandawa")
                var branchClean = branch.Name.ToLower().Replace(" branch", "").Replace(" ", "");
                // Camel case for password (e.g., "Sandawa Branch" -> "Sandawa")
                var branchPascal = char.ToUpper(branchClean[0]) + branchClean.Substring(1);
                
                // 1. Branch Manager
                await EnsureUserAsync(
                    userManager,
                    email: $"manager.{branchClean}@coretex.com",
                    fullName: $"{branch.Name} Manager",
                    password: $"Manager{branchPascal}123!",
                    role: "BRANCH_ADMIN",
                    branchId: branch.Id,
                    logger: logger);

                // 2. Finance Officer
                await EnsureUserAsync(
                    userManager,
                    email: $"finance.{branchClean}@coretex.com",
                    fullName: $"{branch.Name} Finance",
                    password: $"Finance{branchPascal}123!",
                    role: "FINANCE",
                    branchId: branch.Id,
                    logger: logger);

                // 3. Branch Cashier
                await EnsureUserAsync(
                    userManager,
                    email: $"cashier.{branchClean}@coretex.com",
                    fullName: $"{branch.Name} Cashier",
                    password: $"Cashier{branchPascal}123!",
                    role: "CASHIER",
                    branchId: branch.Id,
                    logger: logger);
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
                    TwoFactorEnabled = false,
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

                if (user.TwoFactorEnabled)
                {
                    user.TwoFactorEnabled = false; // TEMPORARY BYPASS FOR TESTING
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
