using ITCompliance.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Existing Tables
        public DbSet<Employee> Employees { get; set; }

        public DbSet<InternetAccessRequest> InternetAccessRequests { get; set; }

        // Company HR View
        public DbSet<OESEmployee> OESEmployees { get; set; }

        // HOD Table
        public DbSet<HODDetail> HODDetails { get; set; }

        // App-owned role grants
        public DbSet<RoleAssignment> RoleAssignments { get; set; }

        // Workflow audit trail and email delivery log
        public DbSet<RequestTransaction> RequestTransactions { get; set; }

        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HR View
            modelBuilder.Entity<OESEmployee>()
                .ToView("OES_vuEmployeeDetails")
                .HasKey(e => e.EmpId);

            // HOD Table
            modelBuilder.Entity<HODDetail>()
                .ToTable("tbl_HODdetails");

            modelBuilder.Entity<RoleAssignment>(entity =>
            {
                entity.Property(r => r.EmployeeId).HasMaxLength(25);
                entity.Property(r => r.Role).HasMaxLength(30);
                entity.Property(r => r.DepartmentCode).HasMaxLength(30);

                entity.HasIndex(r => r.EmployeeId);
                entity.HasIndex(r => new { r.Role, r.DepartmentCode });
            });

            modelBuilder.Entity<InternetAccessRequest>(entity =>
            {
                entity.Property(r => r.DepartmentCode)
                    .HasMaxLength(30)
                    .HasDefaultValue("");

                entity.Property(r => r.PendingDepartmentCode)
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<RequestTransaction>(entity =>
            {
                entity.Property(t => t.ActorEmpId).HasMaxLength(25);
                entity.Property(t => t.ActorRole).HasMaxLength(30);
                entity.Property(t => t.Action).HasMaxLength(20);
                entity.Property(t => t.StageStatus).HasMaxLength(60);

                entity.HasIndex(t => t.RequestId);

                entity.HasOne(t => t.Request)
                    .WithMany()
                    .HasForeignKey(t => t.RequestId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.Property(e => e.RecipientEmail).HasMaxLength(320);
                entity.Property(e => e.Purpose).HasMaxLength(60);

                entity.HasIndex(e => e.RequestId);

                entity.HasOne(e => e.Request)
                    .WithMany()
                    .HasForeignKey(e => e.RequestId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}