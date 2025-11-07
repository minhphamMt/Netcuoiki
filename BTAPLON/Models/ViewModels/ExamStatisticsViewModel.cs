namespace BTAPLON.Models.ViewModels
{
    public class ExamStatisticsViewModel
    {
        public Exam Exam { get; set; }
        public int TotalStudents { get; set; }
        public int TotalSubmissions { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public IDictionary<string, int> GradeDistribution { get; set; } = new Dictionary<string, int>();
    }
}