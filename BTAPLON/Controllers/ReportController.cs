using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using BTAPLON.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize]
    public class ReportController : Controller
    {
        private readonly EduDbContext _context;

        public ReportController(EduDbContext context)
        {
            _context = context;
        }

        [SessionAuthorize("Student")]
        public async Task<IActionResult> Student()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var viewModel = await BuildStudentReportAsync(userId.Value);
            ViewData["Title"] = "Báo cáo học tập";
            return View(viewModel);
        }

        [SessionAuthorize("Student")]
        public async Task<IActionResult> ExportStudent(string format = "pdf")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await BuildStudentReportAsync(userId.Value);
            return CreateExportResult(format, $"student-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                () => ReportExportHelper.StudentReportToPdf(viewModel),
                () => ReportExportHelper.StudentReportToWord(viewModel));
        }

        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> Teacher()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await BuildTeacherReportAsync(userId.Value);
            ViewData["Title"] = "Báo cáo giảng dạy";
            return View(viewModel);
        }

        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> ExportTeacher(string format = "pdf")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await BuildTeacherReportAsync(userId.Value);
            return CreateExportResult(format, $"teacher-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                () => ReportExportHelper.TeacherReportToPdf(viewModel),
                () => ReportExportHelper.TeacherReportToWord(viewModel));
        }

        [SessionAuthorize("Admin")]
        public async Task<IActionResult> Admin()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await BuildAdminReportAsync();
            ViewData["Title"] = "Báo cáo quản trị";
            return View(viewModel);
        }

        [SessionAuthorize("Admin")]
        public async Task<IActionResult> ExportAdmin(string format = "pdf")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await BuildAdminReportAsync();
            return CreateExportResult(format, $"admin-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                () => ReportExportHelper.AdminReportToPdf(viewModel),
                () => ReportExportHelper.AdminReportToWord(viewModel));
        }

        private async Task<StudentLearningReportViewModel> BuildStudentReportAsync(int studentId)
        {
            var classIds = await _context.Enrollments
                .AsNoTracking()
                 .Where(e => e.StudentID == studentId)
                .Select(e => e.ClassID)
                .ToListAsync();

            var totalAssignments = await _context.Assignments
                .AsNoTracking()
                .CountAsync(a => classIds.Contains(a.ClassID));

            var totalExams = await _context.Exams
                .AsNoTracking()
                .CountAsync(e => classIds.Contains(e.ClassID));

            var submissions = await _context.Submissions
                .AsNoTracking()
                 .Where(s => s.StudentID == studentId)
                .Include(s => s.Assignment!)
                    .ThenInclude(a => a.Class!)
                        .ThenInclude(c => c.Course)
                .OrderByDescending(s => s.SubmittedAt ?? DateTime.MinValue)
                .ToListAsync();

            var examSubmissions = await _context.ExamSubmissions
                .AsNoTracking()
              .Where(es => es.StudentID == studentId)
                .Include(es => es.Exam!)
                    .ThenInclude(e => e.Class!)
                        .ThenInclude(c => c.Course)
                .OrderByDescending(es => es.SubmittedAt ?? es.StartedAt)
                .ToListAsync();

            var assignmentScores = submissions
                .Where(s => s.Score.HasValue)
                .Select(s => s.Score!.Value)
                .ToList();

            var examScores = examSubmissions
                .Where(es => es.Score.HasValue)
                .Select(es => es.Score!.Value)
                .ToList();

            return new StudentLearningReportViewModel
            {
                StudentName = HttpContext.Session.GetString("UserEmail") ?? "Học viên",
                TotalAssignments = totalAssignments,
                CompletedAssignments = submissions.Count,
                PendingAssignments = Math.Max(totalAssignments - submissions.Count, 0),
                AverageAssignmentScore = assignmentScores.Any() ? Math.Round((decimal)assignmentScores.Average(), 2) : (decimal?)null,
                TotalExams = totalExams,
                CompletedExams = examSubmissions.Count,
                PendingExams = Math.Max(totalExams - examSubmissions.Count, 0),
                AverageExamScore = examScores.Any() ? Math.Round((decimal)examScores.Average(), 2) : (decimal?)null,
                Assignments = submissions
                    .Select(s => new StudentAssignmentReportItem
                    {
                        AssignmentId = s.AssignmentID,
                        Title = s.Assignment?.Title ?? $"Bài tập #{s.AssignmentID}",
                        ClassCode = s.Assignment?.Class?.ClassCode,
                        CourseName = s.Assignment?.Class?.Course?.CourseName,
                        DueDate = s.Assignment?.DueDate,
                        SubmittedAt = s.SubmittedAt,
                        Score = s.Score
                    })
                    .ToList(),
                Exams = examSubmissions
                    .Select(es => new StudentExamReportItem
                    {
                        ExamId = es.ExamID,
                        Title = es.Exam?.Title ?? $"Kỳ thi #{es.ExamID}",
                        ClassCode = es.Exam?.Class?.ClassCode,
                        CourseName = es.Exam?.Class?.Course?.CourseName,
                        SubmittedAt = es.SubmittedAt ?? es.StartedAt,
                        Score = es.Score
                    })
                    .ToList()
            };

          
        }

        private async Task<TeacherPerformanceReportViewModel> BuildTeacherReportAsync(int teacherId)
        {


            var classes = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Course != null && c.Course.TeacherID == teacherId)
                .Include(c => c.Course)!
                    .ThenInclude(course => course.Teacher)
                .Include(c => c.Enrollments)!
                    .ThenInclude(e => e.Student)
                .Include(c => c.Assignments)!
                    .ThenInclude(a => a.Submissions)
                .Include(c => c.Exams)!
                    .ThenInclude(e => e.Submissions)
                .ToListAsync();

            var classReports = classes.Select(c =>
            {
                var assignmentScores = c.Assignments?
                    .SelectMany(a => a.Submissions ?? new List<Submission>())
                    .Where(s => s.Score.HasValue)
                    .Select(s => (double)s.Score!.Value)
                    .ToList() ?? new List<double>();

                var examScores = c.Exams?
                    .SelectMany(e => e.Submissions ?? new List<ExamSubmission>())
                    .Where(s => s.Score.HasValue)
                    .Select(s => s.Score!.Value)
                    .ToList() ?? new List<double>();

                return new TeacherClassReportItem
                {
                    ClassId = c.ClassID,
                    ClassCode = !string.IsNullOrWhiteSpace(c.ClassCode) ? c.ClassCode : $"Lớp #{c.ClassID}",
                    CourseName = c.Course?.CourseName ?? "Chưa có khóa học",
                    StudentCount = c.Enrollments?.Count ?? 0,
                    AssignmentCount = c.Assignments?.Count ?? 0,
                    ExamCount = c.Exams?.Count ?? 0,
                    AverageAssignmentScore = assignmentScores.Any() ? Math.Round(assignmentScores.Average(), 2) : (double?)null,
                    AverageExamScore = examScores.Any() ? Math.Round(examScores.Average(), 2) : (double?)null
                };
            }).ToList();

            var allAssignmentScores = classes
                .SelectMany(c => c.Assignments ?? new List<Assignment>())
                .SelectMany(a => a.Submissions ?? new List<Submission>())
                .Where(s => s.Score.HasValue)
                .Select(s => (double)s.Score!.Value)
                .ToList();

            var allExamScores = classes
                .SelectMany(c => c.Exams ?? new List<Exam>())
                .SelectMany(e => e.Submissions ?? new List<ExamSubmission>())
                .Where(s => s.Score.HasValue)
                .Select(s => s.Score!.Value)
                .ToList();

            return new TeacherPerformanceReportViewModel
            {
                TeacherName = classes.FirstOrDefault()?.Course?.Teacher?.FullName
                    ?? HttpContext.Session.GetString("UserEmail")
                    ?? "Giảng viên",
                TotalClasses = classes.Count,
                TotalStudents = classes.Sum(c => c.Enrollments?.Count ?? 0),
                AssignmentCount = classes.Sum(c => c.Assignments?.Count ?? 0),
                ExamCount = classes.Sum(c => c.Exams?.Count ?? 0),
                AverageAssignmentScore = allAssignmentScores.Any() ? Math.Round(allAssignmentScores.Average(), 2) : (double?)null,
                AverageExamScore = allExamScores.Any() ? Math.Round(allExamScores.Average(), 2) : (double?)null,
                Classes = classReports
            };
        }
        private async Task<AdminReportViewModel> BuildAdminReportAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
            var totalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher");
            var totalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin");
            var totalCourses = await _context.Courses.CountAsync();
            var totalClasses = await _context.Classes.CountAsync();
            var totalAssignments = await _context.Assignments.CountAsync();
            var totalExams = await _context.Exams.CountAsync();
            var activeExams = await _context.Exams.CountAsync(e => e.IsPublished && (e.EndTime == null || e.EndTime >= DateTime.UtcNow));
            var totalNotifications = await _context.Notifications.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();

            var monthlyStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5);
            var rawMonthly = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.EnrolledAt >= monthlyStart)
                .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyStats = new List<MonthlyEnrollmentStatistic>();
            for (var i = 0; i < 6; i++)
            {
                var date = monthlyStart.AddMonths(i);
                var monthData = rawMonthly.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
                monthlyStats.Add(new MonthlyEnrollmentStatistic
                {
                    Label = date.ToString("MM/yyyy"),
                    EnrollmentCount = monthData?.Count ?? 0
                });
            }

            var topClasses = await _context.Classes
     .AsNoTracking()
     .Include(c => c.Course)
     .Include(c => c.Enrollments)
     .OrderByDescending(c => _context.Enrollments.Count(e => e.ClassID == c.ClassID))
     .Take(5)
     .Select(c => new ClassEnrollmentStatistic
     {
         ClassId = c.ClassID,
         ClassCode = !string.IsNullOrWhiteSpace(c.ClassCode) ? c.ClassCode : $"Lớp #{c.ClassID}",
         CourseName = c.Course != null ? c.Course.CourseName : "Chưa xác định",
         StudentCount = _context.Enrollments.Count(e => e.ClassID == c.ClassID)
     })
     .ToListAsync();


            var teacherLookup = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Teacher")
                .Select(u => new { u.UserID, Name = u.FullName ?? u.Email ?? $"Giảng viên #{u.UserID}" })
                .ToListAsync();

            var classCounts = await _context.Classes
                .Join(_context.Courses,
                    cls => cls.CourseID,
                    course => course.CourseID,
                    (cls, course) => new { cls, course })
                .Where(x => x.course.TeacherID != null)
                .GroupBy(x => x.course.TeacherID!.Value)
                .Select(g => new { TeacherId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeacherId, x => x.Count);

            var assignmentCounts = await _context.Assignments
                .Join(_context.Classes, a => a.ClassID, cls => cls.ClassID, (a, cls) => new { cls.CourseID, AssignmentID = a.AssignmentID })
                .Join(_context.Courses, ac => ac.CourseID, course => course.CourseID, (ac, course) => new { ac.AssignmentID, course.TeacherID })
                .Where(x => x.TeacherID != null)
                .GroupBy(x => x.TeacherID!.Value)
                .Select(g => new { TeacherId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeacherId, x => x.Count);

            var examCounts = await _context.Exams
                .Join(_context.Classes, e => e.ClassID, cls => cls.ClassID, (e, cls) => new { cls.CourseID, e.ExamID })
                .Join(_context.Courses, ec => ec.CourseID, course => course.CourseID, (ec, course) => new { ec.ExamID, course.TeacherID })
                .Where(x => x.TeacherID != null)
                .GroupBy(x => x.TeacherID!.Value)
                .Select(g => new { TeacherId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeacherId, x => x.Count);

            var topTeachers = teacherLookup
                .Select(t => new TeacherActivityStatistic
                {
                    TeacherId = t.UserID,
                    TeacherName = t.Name,
                    ClassCount = classCounts.TryGetValue(t.UserID, out var clsCount) ? clsCount : 0,
                    AssignmentCount = assignmentCounts.TryGetValue(t.UserID, out var aCount) ? aCount : 0,
                    ExamCount = examCounts.TryGetValue(t.UserID, out var eCount) ? eCount : 0
                })
                .OrderByDescending(t => t.TotalActivities)
                .ThenByDescending(t => t.ClassCount)
                .Take(5)
                .ToList();

            return new AdminReportViewModel
            {
                TotalUsers = totalUsers,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalAdmins = totalAdmins,
                TotalCourses = totalCourses,
                TotalClasses = totalClasses,
                TotalAssignments = totalAssignments,
                TotalExams = totalExams,
                ActiveExams = activeExams,
                TotalNotifications = totalNotifications,
                TotalEnrollments = totalEnrollments,
                MonthlyEnrollments = monthlyStats,
                TopClasses = topClasses,
                TopTeachers = topTeachers
            };
        }

        private FileResult CreateExportResult(string format, string fileName, Func<byte[]> pdfFactory, Func<byte[]> wordFactory)
        {
            var normalized = (format ?? "pdf").Trim().ToLowerInvariant();
            return normalized switch
            {
                "word" => File(wordFactory(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{fileName}.docx"),
                _ => File(pdfFactory(), "application/pdf", $"{fileName}.pdf")
            };
        }
    }
}