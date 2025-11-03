namespace BTAPLON.Models
{
    public class Submission
    {
        public int SubmissionID { get; set; }
        public int AssignmentID { get; set; }
        public int StudentID { get; set; }
        public string? FilePath { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }

        public Assignment? Assignment { get; set; }
        public User? Student { get; set; }
    }

}
