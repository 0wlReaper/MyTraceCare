using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Models;

namespace MyTraceCare.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PatientComment> PatientComments { get; set; }
        public DbSet<PatientDataFile> PatientDataFiles { get; set; }
        public DbSet<PatientDevice> PatientDevices { get; set; }
        public DbSet<ClinicianPatient> ClinicianPatients { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Store enums as strings
            builder.Entity<User>()
                .Property(u => u.Gender)
                .HasConversion<string>();

            builder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();


            // ------------------------------
            //  ALERT → USER (many-to-one)
            // ------------------------------
            builder.Entity<Alert>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ------------------------------
            //  ALERT → COMMENTS (one-to-many)
            // ------------------------------
            builder.Entity<Alert>()
                .HasMany(a => a.Comments)
                .WithOne(c => c.Alert)
                .HasForeignKey(c => c.AlertId)
                .OnDelete(DeleteBehavior.Restrict);
            // (Restrict avoids cycles with threaded comments)


            // ------------------------------
            //  COMMENT → USER (many-to-one)
            // ------------------------------
            builder.Entity<PatientComment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ------------------------------
            //  COMMENT → COMMENT (threaded replies)
            // ------------------------------
            builder.Entity<PatientComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(pc => pc.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ------------------------------
            //  PATIENT DEVICE → USER
            // ------------------------------
            builder.Entity<PatientDevice>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClinicianPatient>()
                .HasOne(cp => cp.Clinician)
                .WithMany()
                .HasForeignKey(cp => cp.ClinicianId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ClinicianPatient>()
                .HasOne(cp => cp.Patient)
                .WithMany()
                .HasForeignKey(cp => cp.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
