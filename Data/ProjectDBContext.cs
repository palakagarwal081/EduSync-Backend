using EduSync.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EduSync.Backend.Data
{
    public class ProjectDBContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ProjectDBContext(DbContextOptions<ProjectDBContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Course Entity
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.MediaUrl).IsRequired();

                entity.HasOne(e => e.Instructor)
                    .WithMany(u => u.Courses)
                    .HasForeignKey(e => e.InstructorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Assessment Entity
            modelBuilder.Entity<Assessment>(entity =>
            {
                entity.HasKey(e => e.AssessmentId);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Questions).IsRequired();

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Assessments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Result Entity
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasKey(e => e.ResultId);
                entity.Property(e => e.SubmittedAnswers).IsRequired();
                entity.Property(e => e.Score).IsRequired();
                entity.Property(e => e.AttemptDate).IsRequired();
                entity.Property(e => e.SubmittedAt).IsRequired();

                entity.HasOne(e => e.Assessment)
                    .WithMany(a => a.Results)
                    .HasForeignKey(e => e.AssessmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Results)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Enrollment Entity
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
                entity.Property(e => e.EnrollmentDate).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
