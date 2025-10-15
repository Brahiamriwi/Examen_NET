using Microsoft.EntityFrameworkCore;
using Examen_NET.Models;

namespace Examen_NET.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<EmailHistory> EmailHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // PATIENT CONFIGURATION
            // =====================================================
            modelBuilder.Entity<Patient>(entity =>
            {
                // Índice único en DocumentNumber
                entity.HasIndex(p => p.DocumentNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Patients_DocumentNumber");

                // Relación 1:N con Appointments
                entity.HasMany(p => p.Appointments)
                    .WithOne(a => a.Patient)
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // DOCTOR CONFIGURATION
            // =====================================================
            modelBuilder.Entity<Doctor>(entity =>
            {
                // Índice único en DocumentNumber
                entity.HasIndex(d => d.DocumentNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Doctors_DocumentNumber");

                // Relación 1:N con Appointments
                entity.HasMany(d => d.Appointments)
                    .WithOne(a => a.Doctor)
                    .HasForeignKey(a => a.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // APPOINTMENT CONFIGURATION
            // =====================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                // Índice compuesto para búsquedas de conflictos
                entity.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentTime })
                    .HasDatabaseName("IX_Appointments_DoctorDateTime");

                entity.HasIndex(a => new { a.PatientId, a.AppointmentDate, a.AppointmentTime })
                    .HasDatabaseName("IX_Appointments_PatientDateTime");

                // Relación 1:1 con EmailHistory
                entity.HasOne(a => a.EmailHistory)
                    .WithOne(e => e.Appointment)
                    .HasForeignKey<EmailHistory>(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Valor por defecto para Status
                entity.Property(a => a.Status)
                    .HasDefaultValue("Scheduled");
            });

            // =====================================================
            // EMAIL HISTORY CONFIGURATION
            // =====================================================
            modelBuilder.Entity<EmailHistory>(entity =>
            {
                // Valor por defecto para Status
                entity.Property(e => e.Status)
                    .HasDefaultValue("Sent");

                // Valor por defecto para SentDate
                entity.Property(e => e.SentDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}