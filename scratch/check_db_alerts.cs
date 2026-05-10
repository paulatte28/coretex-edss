using coretex_finalproj.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

public class CheckAlerts {
    public static void Run(ApplicationDbContext context) {
        var count = context.SystemNotifications.Count();
        var lastAlert = context.SystemNotifications.OrderByDescending(n => n.CreatedAt).FirstOrDefault();
        
        Console.WriteLine($"Total Notifications in DB: {count}");
        if (lastAlert != null) {
            Console.WriteLine($"Latest Alert: {lastAlert.Title} - {lastAlert.CreatedAt}");
        } else {
            Console.WriteLine("DB Table is COMPLETELY EMPTY.");
        }
    }
}
