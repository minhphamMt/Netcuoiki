using System.ComponentModel.DataAnnotations;

namespace BTAPLON.Models.ViewModels
{
    public class ExamQuestionFormViewModel
    {
        public int ExamID { get; set; }

        [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc")]
        [StringLength(2000)]
        public string Prompt { get; set; } = string.Empty;

        public bool IsMultipleChoice { get; set; }

        [Range(0, 100)]
        public double Points { get; set; } = 1;

        public List<ChoiceOptionViewModel> Choices { get; set; } = new()
        {
            new ChoiceOptionViewModel(),
            new ChoiceOptionViewModel(),
            new ChoiceOptionViewModel(),
            new ChoiceOptionViewModel()
        };
    }

    public class ChoiceOptionViewModel
    {
        [StringLength(500)]
        public string? Text { get; set; }

        public bool IsCorrect { get; set; }
    }
}