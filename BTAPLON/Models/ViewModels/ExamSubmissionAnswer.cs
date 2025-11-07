namespace BTAPLON.Models.ViewModels
{
    public class ExamSubmissionAnswer
    {
        public int QuestionId { get; set; }
        public int? SelectedChoiceId { get; set; }
        public string? AnswerText { get; set; }
    }
}