using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Models;
using System;

namespace SonghaiHMO.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Enrollee> Enrollees { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<CallCentreStaff> CallCentreStaff { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ClaimAttachment> ClaimAttachments { get; set; }
        public DbSet<EnrolleeUser> EnrolleeUsers { get; set; }
        public DbSet<EmailQueue> EmailQueues { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ScheduledReport> ScheduledReports { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<TwoFactorCode> TwoFactorCodes { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Enrollee)
                .WithMany()
                .HasForeignKey(c => c.EnrolleeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Provider)
                .WithMany()
                .HasForeignKey(c => c.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClaimAttachment>()
                .HasOne(a => a.Claim)
                .WithMany()
                .HasForeignKey(a => a.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EnrolleeUser>()
                .HasOne(e => e.Enrollee)
                .WithMany()
                .HasForeignKey(e => e.EnrolleeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Simple hash function for password
            string SimpleHash(string password)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                    var hash = sha256.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }

            // Seed admin users
            modelBuilder.Entity<AdminUser>().HasData(
                new AdminUser { Id = 1, Username = "admin", Password = "admin123", FullName = "Super Admin", Email = "admin@songhaihmo.com", Role = "SuperAdmin", CreatedAt = DateTime.Now },
                new AdminUser { Id = 2, Username = "viewer", Password = "viewer123", FullName = "Viewer User", Email = "viewer@songhaihmo.com", Role = "Viewer", CreatedAt = DateTime.Now }
            );

            // Seed providers
            modelBuilder.Entity<Provider>().HasData(
                new Provider { Id = 1, ProviderName = "Test Hospital", Email = "provider@test.com", Password = "provider123", Phone = "08012345678", Address = "Lagos, Nigeria", CreatedAt = DateTime.Now },
                new Provider { Id = 2, ProviderName = "City Medical Center", Email = "citymed@test.com", Password = "provider123", Phone = "08087654321", Address = "Abuja, Nigeria", CreatedAt = DateTime.Now },
                new Provider { Id = 3, ProviderName = "Lagos University Teaching Hospital", Email = "luth@test.com", Password = "provider123", Phone = "011234567", Address = "Lagos, Nigeria", CreatedAt = DateTime.Now }
            );

            // Seed call centre staff
            modelBuilder.Entity<CallCentreStaff>().HasData(
                new CallCentreStaff { Id = 1, FullName = "John Agent", Email = "callcentre@songhaihmo.com", Password = "centre123", EmployeeId = "CC001", CreatedAt = DateTime.Now }
            );

            // Seed enrollees
            modelBuilder.Entity<Enrollee>().HasData(
                new Enrollee { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@email.com", Phone = "08011111111", PlanType = "Premium", CreatedAt = DateTime.Now },
                new Enrollee { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@email.com", Phone = "08022222222", PlanType = "Standard", CreatedAt = DateTime.Now },
                new Enrollee { Id = 3, FirstName = "Michael", LastName = "Johnson", Email = "michael.j@email.com", Phone = "08033333333", PlanType = "Basic", CreatedAt = DateTime.Now },
                new Enrollee { Id = 4, FirstName = "Alice", LastName = "Johnson", Email = "alice@email.com", Phone = "08011112222", PlanType = "Premium", CreatedAt = DateTime.Now },
                new Enrollee { Id = 5, FirstName = "Bob", LastName = "Williams", Email = "bob@email.com", Phone = "08033334444", PlanType = "Standard", CreatedAt = DateTime.Now },
                new Enrollee { Id = 6, FirstName = "Carol", LastName = "Brown", Email = "carol@email.com", Phone = "08055556666", PlanType = "Basic", CreatedAt = DateTime.Now }
            );

            // Seed enrollee users (password: enrollee123)
            modelBuilder.Entity<EnrolleeUser>().HasData(
                new EnrolleeUser { Id = 1, EnrolleeId = 1, Username = "JOHNDOE", PasswordHash = SimpleHash("enrollee123"), PasswordSalt = "", CreatedAt = DateTime.Now }
            );

            // Seed claims
            modelBuilder.Entity<Claim>().HasData(
                new Claim { Id = 1, EnrolleeId = 1, ProviderId = 1, ProviderName = "Test Hospital", ServiceDescription = "General Consultation", Amount = 5000, Status = "Approved", AdminNotes = "Approved by admin", SubmittedAt = DateTime.Now.AddDays(-10) },
                new Claim { Id = 2, EnrolleeId = 2, ProviderId = 2, ProviderName = "City Medical Center", ServiceDescription = "Lab Tests", Amount = 15000, Status = "Pending", AdminNotes = "", SubmittedAt = DateTime.Now.AddDays(-5) },
                new Claim { Id = 3, EnrolleeId = 3, ProviderId = 1, ProviderName = "Test Hospital", ServiceDescription = "Pharmacy", Amount = 8000, Status = "Approved", AdminNotes = "Approved", SubmittedAt = DateTime.Now.AddDays(-3) },
                new Claim { Id = 4, EnrolleeId = 4, ProviderId = 2, ProviderName = "City Medical Center", ServiceDescription = "X-Ray", Amount = 25000, Status = "Pending", AdminNotes = "", SubmittedAt = DateTime.Now.AddDays(-2) },
                new Claim { Id = 5, EnrolleeId = 5, ProviderId = 3, ProviderName = "Lagos University Teaching Hospital", ServiceDescription = "Surgery", Amount = 150000, Status = "Approved", AdminNotes = "Pre-authorized", SubmittedAt = DateTime.Now.AddDays(-1) }
            );
        }
    }
}