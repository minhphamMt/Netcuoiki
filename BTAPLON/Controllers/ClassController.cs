using System;
using System.Linq;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize("Admin", "Teacher")]
    public class ClassController : Controller
    {
        public IActionResult Index1()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            return View();
        }

        private readonly EduDbContext _context;

        public ClassController(EduDbContext context)
        {
            _context = context;
        }

        // GET: /Class
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var userId = HttpContext.Session.GetInt32("UserID");

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && userId != null)
            {
                query = query.Where(c => c.Course != null && c.Course.TeacherID == userId);
            }

            var trimmedSearch = searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var normalized = trimmedSearch.ToLower();
                query = query.Where(c =>
                    (!string.IsNullOrEmpty(c.ClassCode) && c.ClassCode.ToLower().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(c.Semester) && c.Semester.ToLower().Contains(normalized)) ||
                    (c.Year != null && c.Year.Value.ToString().Contains(normalized)) ||
                    (c.Course != null && c.Course.CourseName != null && c.Course.CourseName.ToLower().Contains(normalized)));
            }
            var list = await query
                .OrderBy(c => c.ClassCode)
                .ThenBy(c => c.ClassID)
                .ToListAsync();

            ViewBag.CanManage = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            ViewBag.IsTeacher = string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase);
            ViewData["SearchTerm"] = trimmedSearch;
            return View(list);
        }
        // CREATE
        [SessionAuthorize("Admin")]
        public IActionResult Create()
        {
            ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName");
            return View();
        }
        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult Create(Class model)
        {
            if (ModelState.IsValid)
            {
                _context.Classes.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName", model.CourseID);
            return View(model);
        }

        // EDIT GET
        [SessionAuthorize("Admin")]
        public IActionResult Edit(int id)
        {
            var cls = _context.Classes.Find(id);
            if (cls == null) return NotFound();

            ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName", cls.CourseID);
            return View(cls);
        }

        // EDIT POST
        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult Edit(Class model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName", model.CourseID);
                return View(model);
            }

            _context.Classes.Update(model);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // DELETE GET
        [SessionAuthorize("Admin")]
        public IActionResult Delete(int id)
        {
            var cls = _context.Classes.Find(id);
            if (cls == null) return NotFound();

            return View(cls);
        }

        // DELETE POST
        [SessionAuthorize("Admin")]
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var cls = _context.Classes.Find(id);
            if (cls == null) return NotFound();

            _context.Classes.Remove(cls);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
