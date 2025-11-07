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
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Choice> Choices { get; set; }
        public DbSet<ExamSubmission> ExamSubmissions { get; set; }

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
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.ExamsCreated)
                .HasForeignKey(e => e.CreatorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Choice>()
                .HasOne(c => c.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(c => c.QuestionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamSubmission>()
                .HasOne(es => es.Exam)
                .WithMany(e => e.Submissions)
                .HasForeignKey(es => es.ExamID);

            modelBuilder.Entity<ExamSubmission>()
                .HasOne(es => es.Student)
                .WithMany(u => u.ExamSubmissions)
                .HasForeignKey(es => es.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    FullName = "Admin User",
                    Email = "admin@gmail.com",
                   
                    PasswordHash = "$2y$12$YTyoxyxHpp6PDV23yRHRn.4m39bD1zisfhoPdl9dTGaPkEyt8tks.",
                    Role = "Admin",
                   
                    CreatedAt = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

        }
    }

}