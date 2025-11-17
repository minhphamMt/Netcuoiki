using System;
using System.Linq;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize]
    public class AssignmentController : Controller
    {
        public IActionResult Index1()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            return View();
        }

        private readonly EduDbContext _context;

        public AssignmentController(EduDbContext context)
        {
            _context = context;
        }

        // LIST
        public IActionResult Index(string? searchTerm)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            var query = _context.Assignments
               .Include(a => a.Class)!
                   .ThenInclude(c => c.Course)
               .AsQueryable();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                query = query.Where(a =>
                    a.Class != null &&
                    a.Class.Course != null &&
                    a.Class.Course.TeacherID == userId);
            }

            var trimmedSearch = searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var normalized = trimmedSearch.ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(normalized) ||
                    a.AssignmentID.ToString().Contains(normalized) ||
                    (a.Description != null && a.Description.ToLower().Contains(normalized)) ||
                    (a.Class != null && (
                        (!string.IsNullOrEmpty(a.Class.ClassCode) && a.Class.ClassCode.ToLower().Contains(normalized)) ||
                        (a.Class.Course != null && a.Class.Course.CourseName != null && a.Class.Course.CourseName.ToLower().Contains(normalized))
                    )));
            }

            var list = query
                .OrderByDescending(a => a.DueDate ?? DateTime.MaxValue)
                .ThenBy(a => a.AssignmentID)
                .ToList();
            ViewData["SearchTerm"] = trimmedSearch;

            return View(list);
        }

        // CREATE GET
        [SessionAuthorize("Admin", "Teacher")]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            var classQuery = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                classQuery = classQuery.Where(c => c.Course != null && c.Course.TeacherID == userId);
            }

            ViewBag.ClassID = new SelectList(classQuery.ToList(), "ClassID", "ClassCode");
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin", "Teacher")]
        public IActionResult Create(Assignment model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (!ModelState.IsValid)
            {
                var invalidClassQuery = _context.Classes
                    .Include(c => c.Course)
                    .AsQueryable();

                if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
                {
                    invalidClassQuery = invalidClassQuery.Where(c => c.Course != null && c.Course.TeacherID == userId);
                }

                ViewBag.ClassID = new SelectList(invalidClassQuery.ToList(), "ClassID", "ClassCode", model.ClassID);
                return View(model);
            }

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                var teacherClass = _context.Classes
                    .Include(c => c.Course)
                    .FirstOrDefault(c => c.ClassID == model.ClassID);

                if (teacherClass?.Course?.TeacherID != userId)
                {
                    ModelState.AddModelError("ClassID", "Bạn chỉ có thể tạo bài tập cho lớp mà bạn phụ trách.");

                    var restrictedClassQuery = _context.Classes
                        .Include(c => c.Course)
                        .Where(c => c.Course != null && c.Course.TeacherID == userId);

                    ViewBag.ClassID = new SelectList(restrictedClassQuery.ToList(), "ClassID", "ClassCode", model.ClassID);
                    return View(model);
                }
            }

            _context.Assignments.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // EDIT GET
        [SessionAuthorize("Admin", "Teacher")]
        public IActionResult Edit(int id)
        {
            var a = _context.Assignments
                .Include(x => x.Class)
                .ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.AssignmentID == id);

            if (a == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                if (a.Class?.Course?.TeacherID != userId)
                {
                    return Forbid();
                }
            }

            var classQuery = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                classQuery = classQuery.Where(c => c.Course != null && c.Course.TeacherID == userId);
            }

            ViewBag.ClassID = new SelectList(classQuery.ToList(), "ClassID", "ClassCode", a.ClassID);
            return View(a);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin", "Teacher")]
        public IActionResult Edit(Assignment model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            var assignment = _context.Assignments
                .Include(a => a.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefault(a => a.AssignmentID == model.AssignmentID);

            if (assignment == null) return NotFound();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                if (assignment.Class?.Course?.TeacherID != userId)
                {
                    return Forbid();
                }
            }

            if (!ModelState.IsValid)
            {
                var invalidClassQuery = _context.Classes
                    .Include(c => c.Course)
                    .AsQueryable();

                if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
                {
                    invalidClassQuery = invalidClassQuery.Where(c => c.Course != null && c.Course.TeacherID == userId);
                }

                ViewBag.ClassID = new SelectList(invalidClassQuery.ToList(), "ClassID", "ClassCode", model.ClassID);
                return View(model);
            }

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                var targetClass = _context.Classes
                    .Include(c => c.Course)
                    .FirstOrDefault(c => c.ClassID == model.ClassID);

                if (targetClass?.Course?.TeacherID != userId)
                {
                    ModelState.AddModelError("ClassID", "Bạn chỉ có thể chỉnh sửa bài tập cho lớp mà bạn phụ trách.");

                    var restrictedClassQuery = _context.Classes
                        .Include(c => c.Course)
                        .Where(c => c.Course != null && c.Course.TeacherID == userId);

                    ViewBag.ClassID = new SelectList(restrictedClassQuery.ToList(), "ClassID", "ClassCode", model.ClassID);
                    return View(model);
                }
            }

            assignment.Title = model.Title;
            assignment.Description = model.Description;
            assignment.DueDate = model.DueDate;
            assignment.ClassID = model.ClassID;

            _context.Assignments.Update(assignment);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // DELETE
        [SessionAuthorize("Admin", "Teacher")]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            var assignment = _context.Assignments
                .Include(x => x.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.AssignmentID == id);

            if (assignment == null) return NotFound();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                if (assignment.Class?.Course?.TeacherID != userId)
                {
                    return Forbid();
                }
            }

            _context.Assignments.Remove(assignment);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var a = _context.Assignments
                 .Include(x => x.Class)!
                    .ThenInclude(c => c.Course)
                .Include(x => x.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefault(x => x.AssignmentID == id);

            if (a == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                if (a.Class?.Course?.TeacherID != userId)
                {
                    return Forbid();
                }
            }

            return View(a);
        }

        [SessionAuthorize("Student")]
        public async Task<IActionResult> StudentDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Class)!
                    .ThenInclude(c => c.Course)!
                        .ThenInclude(course => course.Teacher)
                .Include(a => a.Class)!
                    .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                return NotFound();
            }

            var isEnrolled = assignment.Class?.Enrollments?.Any(e => e.StudentID == userId) ?? false;
            if (!isEnrolled)
            {
                return Forbid();
            }

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.AssignmentID == id && s.StudentID == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync();

            var viewModel = new StudentAssignmentDetailViewModel
            {
                Assignment = assignment,
                Submission = submission
            };

            ViewData["Title"] = "Chi tiết bài tập";

            return View("StudentDetails", viewModel);
        }
        [SessionAuthorize("Student")]
        public async Task<IActionResult> MyAssignments()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var assignments = await _context.Assignments
                .Where(a => a.Class != null && a.Class.Enrollments.Any(e => e.StudentID == userId))
                .Select(a => new StudentAssignmentListItemViewModel
                {
                    AssignmentID = a.AssignmentID,
                    Title = a.Title,
                    Description = a.Description,
                    DueDate = a.DueDate,
                    ClassCode = a.Class != null ? a.Class.ClassCode : null,
                    CourseName = a.Class != null && a.Class.Course != null ? a.Class.Course.CourseName : null,
                    IsSubmitted = a.Submissions.Any(s => s.StudentID == userId),
                    SubmittedAt = a.Submissions
                        .Where(s => s.StudentID == userId && s.SubmittedAt != null)
                        .OrderByDescending(s => s.SubmittedAt)
                        .Select(s => s.SubmittedAt)
                        .FirstOrDefault()
                })
                .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
                .ThenBy(a => a.AssignmentID)
                .ToListAsync();

            ViewData["Title"] = "Bài tập của tôi";

            return View("MyAssignments", assignments);
        }

    }
}
