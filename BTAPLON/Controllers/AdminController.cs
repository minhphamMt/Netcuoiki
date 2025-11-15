using BTAPLON.Data;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using BTAPLON.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTAPLON.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            return View();
        }

        private readonly EduDbContext _context;

        public AdminController(EduDbContext context)
        {
            _context = context;
        }

        public IActionResult Users(string? searchTerm)
        {
            var query = _context.Users.AsQueryable();

            var trimmedSearch = searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var normalized = trimmedSearch.ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(normalized)) ||
                    (u.Email != null && u.Email.ToLower().Contains(normalized)) ||
                    (u.Role != null && u.Role.ToLower().Contains(normalized)) ||
                    u.UserID.ToString().Contains(normalized));
            }

            var users = query
                .OrderBy(u => u.UserID)
                .ToList();

            ViewData["SearchTerm"] = trimmedSearch;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("UserRole");
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var now = DateTime.UtcNow;
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

            var totalUsers = await _context.Users.AsNoTracking().CountAsync();
            var totalStudents = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "Student");
            var totalTeachers = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "Teacher");
            var totalAdmins = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "Admin");
            var totalCourses = await _context.Courses.AsNoTracking().CountAsync();
            var totalClasses = await _context.Classes.AsNoTracking().CountAsync();
            var totalAssignments = await _context.Assignments.AsNoTracking().CountAsync();
            var totalExams = await _context.Exams.AsNoTracking().CountAsync();
            var activeExams = await _context.Exams.AsNoTracking().CountAsync(e => e.IsPublished && (!e.EndTime.HasValue || e.EndTime > now));
            var totalNotifications = await _context.Notifications.AsNoTracking().CountAsync();
            var totalEnrollments = await _context.Enrollments.AsNoTracking().CountAsync();

            var enrollmentByMonthList = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.EnrolledAt >= startMonth)
                .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var topClasses = await _context.Classes
                .AsNoTracking()
                .Select(c => new ClassEnrollmentStatistic
                {
                    ClassId = c.ClassID,
                    ClassCode = !string.IsNullOrWhiteSpace(c.ClassCode) ? c.ClassCode : $"Lớp #{c.ClassID}",
                    CourseName = c.Course != null && !string.IsNullOrWhiteSpace(c.Course.CourseName)
                        ? c.Course.CourseName
                        : "Chưa có khóa học",
                    StudentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.StudentCount)
                .ThenBy(c => c.CourseName)
                .Take(5)
                .ToListAsync();

            var teacherInfos = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Teacher")
                .Select(u => new { u.UserID, u.FullName, u.Email })
                .ToListAsync();

            var classTeacher = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Course != null && c.Course.TeacherID != null)
                .Select(c => new { TeacherId = c.Course!.TeacherID!.Value })
                .ToListAsync();

            var assignmentCounts = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.Class != null && a.Class.Course != null && a.Class.Course.TeacherID != null)
                .Select(a => new { TeacherId = a.Class!.Course!.TeacherID!.Value })
                .ToListAsync();

            var examCounts = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Class != null && e.Class.Course != null && e.Class.Course.TeacherID != null)
                .Select(e => new { TeacherId = e.Class!.Course!.TeacherID!.Value })
                .ToListAsync();

            var enrollmentByMonth = enrollmentByMonthList
                .ToDictionary(x => (x.Year, x.Month), x => x.Count);

            var monthlyEnrollments = new List<MonthlyEnrollmentStatistic>();
            for (var i = 0; i < 6; i++)
            {
                var month = startMonth.AddMonths(i);
                enrollmentByMonth.TryGetValue((month.Year, month.Month), out var count);
                monthlyEnrollments.Add(new MonthlyEnrollmentStatistic
                {
                    Label = month.ToString("MM/yyyy"),
                    EnrollmentCount = count
                });
            }

            var teacherStats = teacherInfos
                .Select(t => new TeacherActivityStatistic
                {
                    TeacherId = t.UserID,
                    TeacherName = !string.IsNullOrWhiteSpace(t.FullName) ? t.FullName : (t.Email ?? "Giảng viên"),
                    ClassCount = 0,
                    AssignmentCount = 0,
                    ExamCount = 0
                })
                .ToDictionary(t => t.TeacherId);

            foreach (var item in classTeacher)
            {
                if (teacherStats.TryGetValue(item.TeacherId, out var stat))
                {
                    stat.ClassCount++;
                }
            }

            foreach (var item in assignmentCounts)
            {
                if (teacherStats.TryGetValue(item.TeacherId, out var stat))
                {
                    stat.AssignmentCount++;
                }
            }

            foreach (var item in examCounts)
            {
                if (teacherStats.TryGetValue(item.TeacherId, out var stat))
                {
                    stat.ExamCount++;
                }
            }

            var topTeachers = teacherStats.Values
                .Where(t => t.TotalActivities > 0)
                .OrderByDescending(t => t.TotalActivities)
                .ThenBy(t => t.TeacherName)
                .Take(5)
                .ToList();

            var viewModel = new AdminReportViewModel
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
                MonthlyEnrollments = monthlyEnrollments,
                TopClasses = topClasses,
                TopTeachers = topTeachers
            };

            ViewData["Title"] = "Báo cáo thống kê";

            return View(viewModel);
        }
        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash) && !PasswordHelper.IsBcryptHash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
            }

            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Users");
        }
        // ----- EDIT -----
        // AdminController.cs
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            user.PasswordHash = string.Empty;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User model)
        {
            // ignore unrelated navigation
            model.CoursesTaught = null;
            model.Enrollments = null;
            model.Submissions = null;

            var user = _context.Users.FirstOrDefault(u => u.UserID == model.UserID);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Role = model.Role;

            if (!string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                user.PasswordHash = PasswordHelper.IsBcryptHash(model.PasswordHash)
                    ? model.PasswordHash
                    : PasswordHelper.HashPassword(model.PasswordHash);
            }

            _context.SaveChanges();

            return RedirectToAction("Users");
        }

        // ----- DELETE -----
        [HttpGet]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUserConfirmed(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Users");
        }
    }
}