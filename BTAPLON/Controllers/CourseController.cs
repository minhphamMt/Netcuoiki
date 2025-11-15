using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize("Admin")]
    public class CourseController : Controller
    {
        public IActionResult Index1()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            return View();
        }

        private readonly EduDbContext _context;

        public CourseController(EduDbContext context)
        {
            _context = context;
        }

        // GET: /Course
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var query = _context.Courses
                .Include(c => c.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.CourseName.ToLower().Contains(normalized) ||
                    c.CourseID.ToString().Contains(normalized) ||
                    (c.Teacher != null && (
                        (c.Teacher.FullName != null && c.Teacher.FullName.ToLower().Contains(normalized)) ||
                        (c.Teacher.Email != null && c.Teacher.Email.ToLower().Contains(normalized))
                    )));
            }

            var list = await query
                .OrderBy(c => c.CourseName)
                .ThenBy(c => c.CourseID)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm?.Trim();


            return View(list);
        }


        // GET: Course/Create
        public IActionResult Create()
        {
            // load danh sách teacher cho dropdown
            ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList();
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        public IActionResult Create(Course course)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                return View(course);
            }

            course.CreatedAt = DateTime.Now;

            _context.Courses.Add(course);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Course/Edit/5
        public IActionResult Edit(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null) return NotFound();

            ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList();

            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        public IActionResult Edit(Course course)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                return View(course);
            }

            _context.Courses.Update(course);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        // GET: Course/Delete/5
        public IActionResult Delete(int id)
        {
            var course = _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefault(c => c.CourseID == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // POST: Course/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null) return NotFound();

            _context.Courses.Remove(course);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }



    }
}
