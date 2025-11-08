using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models.Forum
{
    public class Answer
    {
        public int AnswerID { get; set; }

        [Required]
        public int QuestionID { get; set; }

        [Required]
        public int AuthorID { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsAccepted { get; set; }

        [ValidateNever]
        public ForumQuestion? Question { get; set; }

        [ValidateNever]
        public User? Author { get; set; }
    }
}