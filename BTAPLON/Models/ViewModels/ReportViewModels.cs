using System;
using System.Collections.Generic;

namespace BTAPLON.Models.ViewModels
{
    public class StudentLearningReportViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public decimal? AverageAssignmentScore { get; set; }
        public int TotalExams { get; set; }
        public int CompletedExams { get; set; }
        public int PendingExams { get; set; }
        public decimal? AverageExamScore { get; set; }
        public IList<StudentAssignmentReportItem> Assignments { get; set; } = new List<StudentAssignmentReportItem>();
        public IList<StudentExamReportItem> Exams { get; set; } = new List<StudentExamReportItem>();
    }

    public class StudentAssignmentReportItem
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ClassCode { get; set; }
        public string? CourseName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
    }

    public class StudentExamReportItem
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ClassCode { get; set; }
        public string? CourseName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public double? Score { get; set; }
    }

    public class TeacherPerformanceReportViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int AssignmentCount { get; set; }
        public int ExamCount { get; set; }
        public double? AverageAssignmentScore { get; set; }
        public double? AverageExamScore { get; set; }
        public IList<TeacherClassReportItem> Classes { get; set; } = new List<TeacherClassReportItem>();
    }

    public class TeacherClassReportItem
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
        public int ExamCount { get; set; }
        public double? AverageAssignmentScore { get; set; }
        public double? AverageExamScore { get; set; }
    }
}