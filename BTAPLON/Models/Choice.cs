using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class Choice
    {
        public int ChoiceID { get; set; }

        [Required]
        public int QuestionID { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        [ValidateNever]
        public Question? Question { get; set; }
    }
}