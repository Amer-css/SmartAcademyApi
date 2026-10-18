using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Models;

namespace SmartAcademyApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // تسجيل الجداول (Entities) لكي يتعرف عليها EF Core وينشئها في SQL Server
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Disable cascading delete for StudentId in the Enrollments table to avoid error 1785
                 modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict); //Prevent nested automatic deletion

            // 2. Applying the soft delete filter (Global Query Filters) to the base tables

          //  modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive == true);
            modelBuilder.Entity<Course>().HasQueryFilter(c => c.IsActive == true);
            modelBuilder.Entity<Enrollment>().HasQueryFilter(e => e.IsActive == true);
        }
    }
}
