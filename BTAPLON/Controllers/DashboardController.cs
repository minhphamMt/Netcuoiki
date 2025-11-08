using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize]
    public class DashboardController : Controller
    {
        private readonly EduDbContext _context;

        public DashboardController(EduDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var overview = new DashboardOverviewViewModel
            {
                Role = role
            };

            var courses = new Dictionary<int, DashboardCourseViewModel>();

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                var classes = await _context.Classes
                    .Include(c => c.Course)!.ThenInclude(course => course.Teacher)
                    .Include(c => c.Assignments)
                    .Include(c => c.Exams)
                    .Where(c => c.Enrollments != null && c.Enrollments.Any(e => e.StudentID == userId))
                    .ToListAsync();

                foreach (var cls in classes)
                {
                    if (cls.Course == null)
                    {
                        continue;
                    }

                    var courseVm = GetOrCreateCourse(courses, cls.Course, false);
                    courseVm.Classes.Add(CreateClassSummary(cls, false));
                }
            }
            else if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                var classes = await _context.Classes
                    .Include(c => c.Course)!.ThenInclude(course => course.Teacher)
                    .Include(c => c.Assignments)
                    .Include(c => c.Exams)
                    .Where(c => c.Course != null && c.Course.TeacherID == userId)
                    .ToListAsync();

                foreach (var cls in classes)
                {
                    if (cls.Course == null)
                    {
                        continue;
                    }

                    var courseVm = GetOrCreateCourse(courses, cls.Course, true);
                    courseVm.Classes.Add(CreateClassSummary(cls, true));
                }

                var additionalCourses = await _context.Courses
                    .Include(c => c.Teacher)
                    .Where(c => c.TeacherID == userId)
                    .ToListAsync();

                foreach (var course in additionalCourses)
                {
                    GetOrCreateCourse(courses, course, true);
                }
            }
            else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var classes = await _context.Classes
                    .Include(c => c.Course)!.ThenInclude(course => course.Teacher)
                    .Include(c => c.Assignments)
                    .Include(c => c.Exams)
                    .ToListAsync();

                foreach (var cls in classes)
                {
                    if (cls.Course == null)
                    {
                        continue;
                    }

                    var courseVm = GetOrCreateCourse(courses, cls.Course, true);
                    courseVm.Classes.Add(CreateClassSummary(cls, true));
                }

                var allCourses = await _context.Courses
                    .Include(c => c.Teacher)
                    .ToListAsync();

                foreach (var course in allCourses)
                {
                    GetOrCreateCourse(courses, course, true);
                }
            }

            await PopulateForumCountsAsync(courses);

            overview.Courses = courses.Values
                .OrderBy(c => c.CourseName)
                .ToList();

            return View(overview);
        }

        public async Task<IActionResult> Class(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CanAccessClassAsync(id, userId.Value, role))
            {
                return Forbid();
            }

            var cls = await _context.Classes
                .Include(c => c.Course)!.ThenInclude(course => course.Teacher)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (cls == null)
            {
                return NotFound();
            }

            var assignments = await _context.Assignments
                .Where(a => a.ClassID == id)
                .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
                .Select(a => new AssignmentSummaryViewModel
                {
                    AssignmentID = a.AssignmentID,
                    Title = a.Title,
                    DueDate = a.DueDate
                })
                .ToListAsync();

            var exams = await _context.Exams
                .Where(e => e.ClassID == id)
                .OrderByDescending(e => e.StartTime ?? e.CreatedAt)
                .Select(e => new ExamSummaryViewModel
                {
                    ExamID = e.ExamID,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    IsPublished = e.IsPublished
                })
                .ToListAsync();

            var threads = await _context.DiscussionThreads
                .Where(t => t.ClassID == id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new ForumThreadSummaryViewModel
                {
                    DiscussionThreadID = t.DiscussionThreadID,
                    Title = t.Title,
                    AuthorName = t.CreatedBy != null ? (t.CreatedBy.FullName ?? t.CreatedBy.Email) : null,
                    CreatedAt = t.CreatedAt,
                    ReplyCount = t.Posts != null ? t.Posts.Count : 0
                })
                .ToListAsync();

            var questions = await _context.ForumQuestions
                .Where(q => q.ClassID == id)
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
                .Select(q => new ForumQuestionSummaryViewModel
                {
                    QuestionID = q.QuestionID,
                    Title = q.Title,
                    StudentName = q.Student != null ? (q.Student.FullName ?? q.Student.Email) : null,
                    CreatedAt = q.CreatedAt,
                    IsResolved = q.IsResolved
                })
                .ToListAsync();

            var canManage = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                            (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) &&
                             cls.Course != null && cls.Course.TeacherID == userId);

            var viewModel = new DashboardClassDetailViewModel
            {
                ClassID = cls.ClassID,
                CourseID = cls.CourseID,
                CourseName = cls.Course?.CourseName ?? string.Empty,
                ClassCode = cls.ClassCode,
                Semester = cls.Semester,
                Year = cls.Year,
                TeacherName = cls.Course?.Teacher?.FullName ?? cls.Course?.Teacher?.Email,
                CanManage = canManage,
                Assignments = assignments,
                Exams = exams,
                Threads = threads,
                Questions = questions
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Course(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            if (!await CanAccessCourseAsync(id, userId.Value, role))
            {
                return Forbid();
            }

            var canManage = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                            (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) && course.TeacherID == userId);

            var classQuery = _context.Classes
                .Include(c => c.Assignments)
                .Include(c => c.Exams)
                .Where(c => c.CourseID == id)
                .AsQueryable();

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                classQuery = classQuery.Where(c => c.Enrollments != null && c.Enrollments.Any(e => e.StudentID == userId));
            }

            var classes = await classQuery
                .Include(c => c.Course)
                .ToListAsync();

            var classSummaries = classes
                .Select(cls => CreateClassSummary(cls, canManage))
                .OrderBy(c => c.ClassCode)
                .ToList();

            var threads = await _context.DiscussionThreads
                .Where(t => t.CourseID == id && t.ClassID == null)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new ForumThreadSummaryViewModel
                {
                    DiscussionThreadID = t.DiscussionThreadID,
                    Title = t.Title,
                    AuthorName = t.CreatedBy != null ? (t.CreatedBy.FullName ?? t.CreatedBy.Email) : null,
                    CreatedAt = t.CreatedAt,
                    ReplyCount = t.Posts != null ? t.Posts.Count : 0
                })
                .ToListAsync();

            var questions = await _context.ForumQuestions
                .Where(q => q.CourseID == id && q.ClassID == null)
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
                .Select(q => new ForumQuestionSummaryViewModel
                {
                    QuestionID = q.QuestionID,
                    Title = q.Title,
                    StudentName = q.Student != null ? (q.Student.FullName ?? q.Student.Email) : null,
                    CreatedAt = q.CreatedAt,
                    IsResolved = q.IsResolved
                })
                .ToListAsync();

            var viewModel = new DashboardCourseDetailViewModel
            {
                CourseID = course.CourseID,
                CourseName = course.CourseName,
                Description = course.Description,
                TeacherName = course.Teacher?.FullName ?? course.Teacher?.Email,
                IsOwner = canManage,
                Classes = classSummaries,
                Threads = threads,
                Questions = questions
            };

            return View(viewModel);
        }

        private DashboardCourseViewModel GetOrCreateCourse(IDictionary<int, DashboardCourseViewModel> lookup, Course course, bool isOwner)
        {
            if (!lookup.TryGetValue(course.CourseID, out var courseVm))
            {
                courseVm = new DashboardCourseViewModel
                {
                    CourseID = course.CourseID,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    TeacherName = course.Teacher?.FullName ?? course.Teacher?.Email,
                    IsOwner = isOwner
                };

                lookup.Add(course.CourseID, courseVm);
            }
            else if (isOwner)
            {
                courseVm.IsOwner = true;
            }

            if (string.IsNullOrWhiteSpace(courseVm.TeacherName) && course.Teacher != null)
            {
                courseVm.TeacherName = course.Teacher.FullName ?? course.Teacher.Email;
            }

            return courseVm;
        }

        private DashboardClassSummaryViewModel CreateClassSummary(Class cls, bool canManage)
        {
            var assignments = cls.Assignments ?? new List<Assignment>();
            var exams = cls.Exams ?? new List<Exam>();
            var now = DateTime.Now;

            var nextExam = exams
                .Where(e => e.IsPublished && e.StartTime.HasValue && e.StartTime.Value >= now)
                .OrderBy(e => e.StartTime)
                .FirstOrDefault();

            return new DashboardClassSummaryViewModel
            {
                ClassID = cls.ClassID,
                ClassCode = cls.ClassCode,
                Semester = cls.Semester,
                Year = cls.Year,
                AssignmentCount = assignments.Count,
                ExamCount = exams.Count,
                NextExamStartTime = nextExam?.StartTime,
                CanManage = canManage
            };
        }

        private async Task PopulateForumCountsAsync(IDictionary<int, DashboardCourseViewModel> courses)
        {
            if (!courses.Any())
            {
                return;
            }

            var courseIds = courses.Keys.ToList();

            var threadCounts = await _context.DiscussionThreads
                .Where(t => t.CourseID != null && courseIds.Contains(t.CourseID.Value) && t.ClassID == null)
                .GroupBy(t => t.CourseID!.Value)
                .Select(g => new { CourseID = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var entry in threadCounts)
            {
                if (courses.TryGetValue(entry.CourseID, out var courseVm))
                {
                    courseVm.ForumThreadCount = entry.Count;
                }
            }

            var questionCounts = await _context.ForumQuestions
                .Where(q => q.CourseID != null && courseIds.Contains(q.CourseID.Value) && q.ClassID == null)
                .GroupBy(q => q.CourseID!.Value)
                .Select(g => new { CourseID = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var entry in questionCounts)
            {
                if (courses.TryGetValue(entry.CourseID, out var courseVm))
                {
                    courseVm.ForumQuestionCount = entry.Count;
                }
            }
        }

        private async Task<bool> CanAccessClassAsync(int classId, int userId, string role)
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Classes
                    .Where(c => c.ClassID == classId)
                    .AnyAsync(c => c.Course != null && c.Course.TeacherID == userId);
            }

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.ClassID == classId && e.StudentID == userId);
            }

            return false;
        }

        private async Task<bool> CanAccessCourseAsync(int courseId, int userId, string role)
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Courses
                    .AnyAsync(c => c.CourseID == courseId && c.TeacherID == userId);
            }

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.StudentID == userId && e.Class != null && e.Class.CourseID == courseId);
            }

            return false;
        }
    }
}