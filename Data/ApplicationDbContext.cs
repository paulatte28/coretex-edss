using coretex_finalproj.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace coretex_finalproj.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<KpiThreshold> KpiThresholds { get; set; } = null!;
        public DbSet<GoalTarget> GoalTargets { get; set; } = null!;
        public DbSet<ReportSchedule> ReportSchedules { get; set; } = null!;
        public DbSet<ActivityLogEntry> ActivityLogs { get; set; } = null!;
        public DbSet<BranchSubmission> BranchSubmissions { get; set; } = null!;
        public DbSet<GeneratedReport> GeneratedReports { get; set; } = null!;
        public DbSet<DailySummary> DailySummaries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Branch>()
                .HasIndex(b => b.BranchCode)
                .IsUnique();

            builder.Entity<AppUser>()
                .HasOne(u => u.Branch)
                .WithMany()
                .HasForeignKey(u => u.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasOne(p => p.Branch)
                .WithMany()
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
    
            builder.Entity<Product>()
                .Property(p => p.Price)
                 .HasPrecision(18, 2);

            builder.Entity<Expense>()
                .HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Sale>()
                .HasOne(s => s.Branch)
                .WithMany()
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Sale>()
                .Property(s => s.Amount)
                .HasPrecision(18, 2);

            builder.Entity<KpiThreshold>()
                .Property(t => t.MinProfitMargin)
                .HasPrecision(5, 2);

            builder.Entity<KpiThreshold>()
                .Property(t => t.MaxExpenseRatio)
                .HasPrecision(5, 2);

            builder.Entity<KpiThreshold>()
                .Property(t => t.MinMonthlyProfit)
                .HasPrecision(18, 2);

            builder.Entity<GoalTarget>()
                .Property(g => g.TargetValue)
                .HasPrecision(18, 2);

            builder.Entity<KpiThreshold>()
                .HasOne(t => t.Branch)
                .WithMany()
                .HasForeignKey(t => t.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<KpiThreshold>()
                .HasIndex(t => new { t.BranchId, t.IsActive });

            builder.Entity<GoalTarget>()
                .HasOne(g => g.Branch)
                .WithMany()
                .HasForeignKey(g => g.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GoalTarget>()
                .HasIndex(g => new { g.BranchId, g.MetricName, g.IsActive });

            builder.Entity<ReportSchedule>()
                .HasOne(r => r.Branch)
                .WithMany()
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReportSchedule>()
                .HasIndex(r => new { r.BranchId, r.IsEnabled });

            builder.Entity<ActivityLogEntry>()
                .HasOne(a => a.Branch)
                .WithMany()
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ActivityLogEntry>()
                .HasIndex(a => new { a.BranchId, a.CreatedAt });

            builder.Entity<BranchSubmission>()
                .HasIndex(s => new { s.BranchId, s.SubmissionYear, s.SubmissionMonth });

            builder.Entity<BranchSubmission>()
                .HasOne(s => s.Branch)
                .WithMany()
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GeneratedReport>()
                .HasOne(r => r.Branch)
                .WithMany()
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<GeneratedReport>()
                .HasOne(r => r.GeneratedBy)
                .WithMany()
                .HasForeignKey(r => r.GeneratedById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<DailySummary>()
                .Property(d => d.TotalRevenue).HasPrecision(18, 2);
            builder.Entity<DailySummary>()
                .Property(d => d.TotalExpenses).HasPrecision(18, 2);
            builder.Entity<DailySummary>()
                .Property(d => d.NetProfit).HasPrecision(18, 2);
        }
    }
}
