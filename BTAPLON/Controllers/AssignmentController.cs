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
        public IActionResult Index()
        {
            var list = _context.Assignments
                .Include(a => a.Class)
                .ToList();

            return View(list);
        }

        // CREATE GET
        [SessionAuthorize("Admin")]
        public IActionResult Create()
        {
            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode");
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult Create(Assignment model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", model.ClassID);
                return View(model);
            }

            _context.Assignments.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // EDIT GET
        [SessionAuthorize("Admin")]
        public IActionResult Edit(int id)
        {
            var a = _context.Assignments
                .Include(x => x.Class)
                .FirstOrDefault(x => x.AssignmentID == id);

            if (a == null) return NotFound();

            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", a.ClassID);
            return View(a);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult Edit(Assignment model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", model.ClassID);
                return View(model);
            }

            _context.Assignments.Update(model);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // DELETE
        [SessionAuthorize("Admin")]
        public IActionResult Delete(int id)
        {
            var a = _context.Assignments.Find(id);
            if (a == null) return NotFound();

            _context.Assignments.Remove(a);
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

            return View(a);
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
