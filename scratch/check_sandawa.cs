using coretex_finalproj.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

// Use this script to check Sandawa branch stats
public class CheckStats {
    public static void Run(ApplicationDbContext context) {
        var branch = context.Branches.FirstOrDefault(b => b.Name.Contains("Sandawa"));
        if (branch == null) {
            Console.WriteLine("Sandawa branch not found.");
            return;
        }
        
        var sales = context.Sales.Where(s => s.BranchId == branch.Id && !s.IsArchived).Sum(s => (decimal?)s.Amount) ?? 0;
        var expenses = context.Expenses.Where(e => e.BranchId == branch.Id && !e.IsArchived).Sum(e => (decimal?)e.Amount) ?? 0;
        var profit = sales - expenses;
        var margin = sales > 0 ? (profit / sales) * 100 : 0;
        
        Console.WriteLine($"Branch: {branch.Name}");
        Console.WriteLine($"ID: {branch.Id}");
        Console.WriteLine($"Sales: {sales}");
        Console.WriteLine($"Expenses: {expenses}");
        Console.WriteLine($"Current Margin: {margin}%");
    }
}
