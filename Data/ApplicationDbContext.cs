using BloodBank.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodBank.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<BloodCenter> BloodCenters { get; set; }
        public DbSet<MedicalQuestionnaire> MedicalQuestionnaires { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<BloodUnit> BloodUnits { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<AppointmentBloodUnit> AppointmentBloodUnits { get; set; }
        public DbSet<BloodUnitRequest> BloodUnitRequests { get; set; }
        public DbSet<SMSNotification> SMSNotifications { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<Ministry> Ministries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent cascade delete cycles
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
