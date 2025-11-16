using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
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

            var classIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentID == userId)
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
                .Where(s => s.StudentID == userId)
                .Include(s => s.Assignment!)
                    .ThenInclude(a => a.Class!)
                        .ThenInclude(c => c.Course)
                .OrderByDescending(s => s.SubmittedAt ?? DateTime.MinValue)
                .ToListAsync();

            var examSubmissions = await _context.ExamSubmissions
                .AsNoTracking()
                .Where(es => es.StudentID == userId)
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

            var assignmentItems = submissions
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
                .ToList();

            var examItems = examSubmissions
                .Select(es => new StudentExamReportItem
                {
                    ExamId = es.ExamID,
                    Title = es.Exam?.Title ?? $"Kỳ thi #{es.ExamID}",
                    ClassCode = es.Exam?.Class?.ClassCode,
                    CourseName = es.Exam?.Class?.Course?.CourseName,
                    SubmittedAt = es.SubmittedAt ?? es.StartedAt,
                    Score = es.Score
                })
                .ToList();

            var viewModel = new StudentLearningReportViewModel
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
                Assignments = assignmentItems,
                Exams = examItems
            };

            ViewData["Title"] = "Báo cáo học tập";

            return View(viewModel);
        }

        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> Teacher()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var classes = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Course != null && c.Course.TeacherID == userId)
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

            var viewModel = new TeacherPerformanceReportViewModel
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

            ViewData["Title"] = "Báo cáo giảng dạy";

            return View(viewModel);
        }
    }
}