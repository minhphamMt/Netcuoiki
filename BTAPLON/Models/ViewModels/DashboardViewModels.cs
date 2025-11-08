using System;
using System.Collections.Generic;

namespace BTAPLON.Models.ViewModels
{
    public class DashboardOverviewViewModel
    {
        public string Role { get; set; } = string.Empty;
        public IList<DashboardCourseViewModel> Courses { get; set; } = new List<DashboardCourseViewModel>();
    }

    public class DashboardCourseViewModel
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TeacherName { get; set; }
        public bool IsOwner { get; set; }
        public int ForumThreadCount { get; set; }
        public int ForumQuestionCount { get; set; }
        public IList<DashboardClassSummaryViewModel> Classes { get; set; } = new List<DashboardClassSummaryViewModel>();
    }

    public class DashboardClassSummaryViewModel
    {
        public int ClassID { get; set; }
        public string? ClassCode { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public int AssignmentCount { get; set; }
        public int ExamCount { get; set; }
        public DateTime? NextExamStartTime { get; set; }
        public bool CanManage { get; set; }
    }

    public class DashboardCourseDetailViewModel
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TeacherName { get; set; }
        public bool IsOwner { get; set; }
        public IList<DashboardClassSummaryViewModel> Classes { get; set; } = new List<DashboardClassSummaryViewModel>();
        public IList<ForumThreadSummaryViewModel> Threads { get; set; } = new List<ForumThreadSummaryViewModel>();
        public IList<ForumQuestionSummaryViewModel> Questions { get; set; } = new List<ForumQuestionSummaryViewModel>();
    }

    public class DashboardClassDetailViewModel
    {
        public int ClassID { get; set; }
        public int? CourseID { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? ClassCode { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public string? TeacherName { get; set; }
        public bool CanManage { get; set; }
        public IList<AssignmentSummaryViewModel> Assignments { get; set; } = new List<AssignmentSummaryViewModel>();
        public IList<ExamSummaryViewModel> Exams { get; set; } = new List<ExamSummaryViewModel>();
        public IList<ForumThreadSummaryViewModel> Threads { get; set; } = new List<ForumThreadSummaryViewModel>();
        public IList<ForumQuestionSummaryViewModel> Questions { get; set; } = new List<ForumQuestionSummaryViewModel>();
    }

    public class AssignmentSummaryViewModel
    {
        public int AssignmentID { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }

    public class ExamSummaryViewModel
    {
        public int ExamID { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsPublished { get; set; }
    }

    public class ForumThreadSummaryViewModel
    {
        public int DiscussionThreadID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReplyCount { get; set; }
    }

    public class ForumQuestionSummaryViewModel
    {
        public int QuestionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
    }
}