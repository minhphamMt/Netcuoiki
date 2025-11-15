using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace BTAPLON.Controllers
{
    [SessionAuthorize("Admin")]
    public class EnrollmentController : Controller
    {
        public IActionResult Index1()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            return View();
        }

        private readonly EduDbContext _context;

        public EnrollmentController(EduDbContext context)
        {
            _context = context;
        }

        // LIST
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var query = _context.Enrollments
                 .Include(e => e.Class)!
                     .ThenInclude(c => c.Course)
                 .Include(e => e.Student)
                  .AsQueryable();

            var trimmedSearch = searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var normalized = trimmedSearch.ToLower();
                query = query.Where(e =>
                    e.EnrollmentID.ToString().Contains(normalized) ||
                    (e.Class != null && (
                        (!string.IsNullOrEmpty(e.Class.ClassCode) && e.Class.ClassCode.ToLower().Contains(normalized)) ||
                        (e.Class.Course != null && e.Class.Course.CourseName != null && e.Class.Course.CourseName.ToLower().Contains(normalized))
                    )) ||
                    (e.Student != null && (
                        (e.Student.FullName != null && e.Student.FullName.ToLower().Contains(normalized)) ||
                        (e.Student.Email != null && e.Student.Email.ToLower().Contains(normalized))
                    )));
            }

            var list = await query
                .OrderByDescending(e => e.EnrolledAt)
                .ThenBy(e => e.EnrollmentID)
                .ToListAsync();

            ViewData["SearchTerm"] = trimmedSearch;
            return View(list);
        }


        // CREATE GET
        public IActionResult Create()
        {
            var model = PopulateOptions(new EnrollmentBulkCreateViewModel());
            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EnrollmentBulkCreateViewModel model)
        {
            model.StudentIDs ??= new List<int>();

            if (!model.StudentIDs.Any())
            {
                ModelState.AddModelError("StudentIDs", "Please select at least one student.");
            }

            if (ModelState.IsValid && model.ClassID.HasValue)
            {
                var existingStudentIds = _context.Enrollments
                    .Where(e => e.ClassID == model.ClassID && e.StudentID.HasValue && model.StudentIDs.Contains(e.StudentID.Value))
                    .Select(e => e.StudentID!.Value)
                    .ToHashSet();

                var newEnrollments = model.StudentIDs
                    .Where(id => !existingStudentIds.Contains(id))
                    .Select(id => new Enrollment
                    {
                        ClassID = model.ClassID,
                        StudentID = id,
                        EnrolledAt = DateTime.Now
                    })
                    .ToList();

                if (newEnrollments.Any())
                {
                    _context.Enrollments.AddRange(newEnrollments);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("StudentIDs", "All selected students are already enrolled in this class.");
            }

            PopulateOptions(model);
            return View(model);
        }

        private EnrollmentBulkCreateViewModel PopulateOptions(EnrollmentBulkCreateViewModel model)
        {
            model.ClassOptions = _context.Classes
                .Select(c => new SelectListItem
                {
                    Value = c.ClassID.ToString(),
                    Text = c.ClassCode
                })
                .ToList();

            model.StudentOptions = _context.Users
                .Where(u => u.Role == "Student")
                .Select(s => new SelectListItem
                {
                    Value = s.UserID.ToString(),
                    Text = s.FullName
                })
                .ToList();

            return model;
        
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var enrollment = _context.Enrollments.Find(id);
            if (enrollment == null)
            {
                return NotFound();
            }

            _context.Enrollments.Remove(enrollment);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Enrollment/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _context.Enrollments.Find(id);
            if (model == null) return NotFound();

            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", model.ClassID);
            ViewBag.StudentID = new SelectList(_context.Users.Where(u => u.Role == "Student"), "UserID", "FullName", model.StudentID);
            return View(model);
        }

        // POST: Enrollment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Enrollment model)
        {
            if (ModelState.IsValid)
            {
                _context.Enrollments.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", model.ClassID);
            ViewBag.StudentID = new SelectList(_context.Users.Where(u => u.Role == "Student"), "UserID", "FullName", model.StudentID);
            return View(model);


        }
    }
}
