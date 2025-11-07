using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class ExamSubmission
    {
        public int ExamSubmissionID { get; set; }

        [Required]
        public int ExamID { get; set; }

        [Required]
        public int StudentID { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }

        public double? Score { get; set; }

        public string? TeacherFeedback { get; set; }

        public string? AnswersJson { get; set; }

        public bool IsFinalized { get; set; }

        [ValidateNever]
        public Exam? Exam { get; set; }

        [ValidateNever]
        public User? Student { get; set; }
    }
}