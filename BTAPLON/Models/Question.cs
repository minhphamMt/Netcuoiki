using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class Question
    {
        public int QuestionID { get; set; }

        [Required]
        public int ExamID { get; set; }

        [Required]
        [StringLength(2000)]
        public string Prompt { get; set; } = string.Empty;

        public bool IsMultipleChoice { get; set; }

        [Range(0, 100)]
        public double Points { get; set; } = 1;

        public int DisplayOrder { get; set; }

        [ValidateNever]
        public Exam? Exam { get; set; }

        [ValidateNever]
        public ICollection<Choice>? Choices { get; set; }
    }
}