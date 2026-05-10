using coretex_finalproj.Data;
using coretex_finalproj.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

public class UpdateManager {
    public static async Task Run(ApplicationDbContext context, UserManager<AppUser> userManager) {
        // Find the manager user (assuming there's one with BRANCH_ADMIN role)
        var users = await userManager.GetUsersInRoleAsync("BRANCH_ADMIN");
        var manager = users.FirstOrDefault();
        
        if (manager == null) {
            Console.WriteLine("No Branch Manager found to update.");
            return;
        }

        string oldName = manager.UserName;
        string newEmail = "manager.sandawa@coretex.com";
        
        manager.Email = newEmail;
        manager.UserName = newEmail;
        manager.NormalizedEmail = newEmail.ToUpper();
        manager.NormalizedUserName = newEmail.ToUpper();

        var result = await userManager.UpdateAsync(manager);
        if (result.Succeeded) {
            Console.WriteLine($"SUCCESS: Renamed {oldName} to {newEmail}");
        } else {
            Console.WriteLine($"FAILED: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
