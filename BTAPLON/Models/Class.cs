using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;


namespace BTAPLON.Models
{
    public class Class
    {
        public int ClassID { get; set; }

        [Required]
        public int CourseID { get; set; }

        public string ClassCode { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public int? Year { get; set; }


        [ValidateNever]
        public Course? Course { get; set; }

        [ValidateNever]
        public ICollection<Enrollment>? Enrollments { get; set; }

        [ValidateNever]
        public ICollection<Assignment>? Assignments { get; set; }

        [ValidateNever]
        public ICollection<Exam>? Exams { get; set; }

        [ValidateNever]
        public ICollection<Forum.DiscussionThread>? DiscussionThreads { get; set; }

        [ValidateNever]
        public ICollection<Forum.ForumQuestion>? Questions { get; set; }
        [ValidateNever]
        public ICollection<Notification>? Notifications { get; set; }
    }
}
