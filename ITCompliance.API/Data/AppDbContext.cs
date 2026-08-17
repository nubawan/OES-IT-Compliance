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
        }
    }
}