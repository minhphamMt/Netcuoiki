using Microsoft.EntityFrameworkCore;
using BTAPLON.Models;
using BTAPLON.Models.Forum;

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
        public DbSet<DiscussionThread> DiscussionThreads { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<ForumQuestion> ForumQuestions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    
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

            modelBuilder.Entity<DiscussionThread>()
                .HasOne(dt => dt.Course)
                .WithMany(c => c.DiscussionThreads!)
                .HasForeignKey(dt => dt.CourseID)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DiscussionThread>()
                .HasOne(dt => dt.Class)
                .WithMany(c => c.DiscussionThreads!)
                .HasForeignKey(dt => dt.ClassID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DiscussionThread>()
                .HasOne(dt => dt.CreatedBy)
                .WithMany(u => u.DiscussionThreads!)
                .HasForeignKey(dt => dt.CreatedByID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.DiscussionThread)
                .WithMany(dt => dt.Posts)
                .HasForeignKey(p => p.DiscussionThreadID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.ForumPosts!)
                .HasForeignKey(p => p.AuthorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Questions!)
                .HasForeignKey(q => q.CourseID)
                 .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.Class)
                .WithMany(c => c.Questions!)
                .HasForeignKey(q => q.ClassID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.Student)
                .WithMany(u => u.QuestionsAsked!)
                .HasForeignKey(q => q.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Author)
                .WithMany(u => u.AnswersProvided!)
                .HasForeignKey(a => a.AuthorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
               .HasOne(n => n.Class)
                .WithMany(c => c.Notifications!)
                .HasForeignKey(n => n.ClassID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.CreatedBy)
                .WithMany(u => u.NotificationsCreated!)
                .HasForeignKey(n => n.CreatedByID)
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