using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BTAPLON.Models
{
    public class Class
    {
        public int ClassID { get; set; }

        [Required]
        public int CourseID { get; set; }

        public string? ClassCode { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }

        [ValidateNever]
        public Course? Course { get; set; }

        [ValidateNever]
        public ICollection<Enrollment>? Enrollments { get; set; }

        [ValidateNever]
        public ICollection<Assignment>? Assignments { get; set; }

        [ValidateNever]
        public ICollection<Exam>? Exams { get; set; }
    }
}
