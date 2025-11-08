namespace BTAPLON.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; } // Admin, Teacher, Student
        public DateTime CreatedAt { get; set; }

        public ICollection<Course>? CoursesTaught { get; set; } // nếu Teacher
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Submission>? Submissions { get; set; }
        public ICollection<Exam>? ExamsCreated { get; set; }
        public ICollection<ExamSubmission>? ExamSubmissions { get; set; }
        public ICollection<Forum.DiscussionThread>? DiscussionThreads { get; set; }
        public ICollection<Forum.Post>? ForumPosts { get; set; }
        public ICollection<Forum.ForumQuestion>? QuestionsAsked { get; set; }
        public ICollection<Forum.Answer>? AnswersProvided { get; set; }
    }

}
