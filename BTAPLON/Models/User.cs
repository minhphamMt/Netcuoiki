using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; } 
        public DateTime CreatedAt { get; set; }

        [ValidateNever]
        public ICollection<Course>? CoursesTaught { get; set; }

        [ValidateNever]
        public ICollection<Enrollment>? Enrollments { get; set; }
        [ValidateNever]
        public ICollection<Submission>? Submissions { get; set; }
        [ValidateNever]
        public ICollection<Exam>? ExamsCreated { get; set; }
        [ValidateNever]
        public ICollection<ExamSubmission>? ExamSubmissions { get; set; }
        [ValidateNever]
        public ICollection<Forum.DiscussionThread>? DiscussionThreads { get; set; }
        [ValidateNever]
        public ICollection<Forum.Post>? ForumPosts { get; set; }
        [ValidateNever]
        public ICollection<Forum.ForumQuestion>? QuestionsAsked { get; set; }
        [ValidateNever]
        public ICollection<Forum.Answer>? AnswersProvided { get; set; }
        [ValidateNever]
        public ICollection<Notification>? NotificationsCreated { get; set; }
     
    }

}
