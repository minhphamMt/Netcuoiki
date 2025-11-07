namespace BTAPLON.Models.ViewModels
{
    public class ExamTakeViewModel
    {
        public Exam Exam { get; set; }
        public ExamSubmission Submission { get; set; }
        public IDictionary<int, ExamSubmissionAnswer> Answers { get; set; } = new Dictionary<int, ExamSubmissionAnswer>();
        public int RemainingSeconds { get; set; }
    }
}