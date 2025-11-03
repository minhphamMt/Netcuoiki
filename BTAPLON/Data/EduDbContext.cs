using Microsoft.EntityFrameworkCore;
using BTAPLON.Models;

namespace BTAPLON.Data
{
    public class EduDbContext : DbContext
    {
        public EduDbContext(DbContextOptions<EduDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Course>()
            .HasOne(c => c.Teacher)
            .WithMany(u => u.CoursesTaught)
            .HasForeignKey(c => c.TeacherID)
            .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)                 
                .WithMany(u => u.Enrollments)          
                .HasForeignKey(e => e.StudentID);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    FullName = "Admin User",
                    Email = "admin@gmail.com",
                    PasswordHash = "123",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                }
            );

        }
    }

}
