using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize("Admin")]
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
        public async Task<IActionResult> Index()
        {
            var list = await _context.Classes
                .Include(c => c.Course)
                .ToListAsync();

            return View(list);
        }
        // CREATE
        public IActionResult Create()
        {
            ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName");
            return View();
        }
        [HttpPost]
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
        public IActionResult Edit(int id)
        {
            var cls = _context.Classes.Find(id);
            if (cls == null) return NotFound();

            ViewBag.CourseID = new SelectList(_context.Courses, "CourseID", "CourseName", cls.CourseID);
            return View(cls);
        }

        // EDIT POST
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
        public IActionResult Delete(int id)
        {
            var cls = _context.Classes.Find(id);
            if (cls == null) return NotFound();

            return View(cls);
        }

        // DELETE POST
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
