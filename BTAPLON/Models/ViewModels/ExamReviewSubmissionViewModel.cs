namespace BTAPLON.Models.ViewModels
{
    public class ExamReviewSubmissionViewModel
    {
        public Exam Exam { get; set; }
        public ExamSubmission Submission { get; set; }
        public IList<ExamSubmissionAnswer> Answers { get; set; } = new List<ExamSubmissionAnswer>();
    }
}