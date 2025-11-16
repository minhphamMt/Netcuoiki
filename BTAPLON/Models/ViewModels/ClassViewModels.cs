using System;
using System.Collections.Generic;
using System.Linq;

namespace BTAPLON.Models.ViewModels
{
    public class ClassDetailViewModel
    {
        public Class Class { get; set; } = null!;
        public string CourseName => Class.Course?.CourseName ?? "Chưa cập nhật";
        public string TeacherName => Class.Course?.Teacher?.FullName
            ?? Class.Course?.Teacher?.Email
            ?? "Chưa có giảng viên";
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
        public int ExamCount { get; set; }
        public IEnumerable<Assignment> UpcomingAssignments { get; set; } = Enumerable.Empty<Assignment>();
        public IEnumerable<Exam> UpcomingExams { get; set; } = Enumerable.Empty<Exam>();
        public IEnumerable<User> Students { get; set; } = Enumerable.Empty<User>();
    }
}