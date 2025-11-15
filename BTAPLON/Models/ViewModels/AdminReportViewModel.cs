using System.Collections.Generic;

namespace BTAPLON.Models.ViewModels
{
    public class AdminReportViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalCourses { get; set; }
        public int TotalClasses { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalExams { get; set; }
        public int ActiveExams { get; set; }
        public int TotalNotifications { get; set; }
        public int TotalEnrollments { get; set; }

        public IList<MonthlyEnrollmentStatistic> MonthlyEnrollments { get; set; } = new List<MonthlyEnrollmentStatistic>();
        public IList<ClassEnrollmentStatistic> TopClasses { get; set; } = new List<ClassEnrollmentStatistic>();
        public IList<TeacherActivityStatistic> TopTeachers { get; set; } = new List<TeacherActivityStatistic>();
    }

    public class MonthlyEnrollmentStatistic
    {
        public string Label { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
    }

    public class ClassEnrollmentStatistic
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class TeacherActivityStatistic
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int ClassCount { get; set; }
        public int AssignmentCount { get; set; }
        public int ExamCount { get; set; }
        public int TotalActivities => ClassCount + AssignmentCount + ExamCount;
    }
}