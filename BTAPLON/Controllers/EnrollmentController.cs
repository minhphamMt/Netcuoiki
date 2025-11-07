using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        public async Task<IActionResult> Index()
        {
            var list = await _context.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Student)
                .ToListAsync();

            return View(list);
        }


        // CREATE GET
        public IActionResult Create()
        {
            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode");
            ViewBag.StudentID = new SelectList(_context.Users.Where(u => u.Role == "Student"), "UserID", "FullName");
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Enrollment model)
        {
            var classValue = Request.Form["ClassID"];
            var studentValue = Request.Form["StudentID"];
            Console.WriteLine($"POST ClassID={classValue}, StudentID={studentValue}");

            if (ModelState.IsValid)
            {
                model.EnrolledAt = DateTime.Now;
                _context.Enrollments.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode", model.ClassID);
            ViewBag.StudentID = new SelectList(_context.Users.Where(u => u.Role == "Student"), "UserID", "FullName", model.StudentID);
            return View(model);
        }



        // DELETE
        public IActionResult Delete(int id)
        {
            var e = _context.Enrollments.Find(id);
            if (e == null) return NotFound();

            _context.Enrollments.Remove(e);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Enrollment/Edit/5
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
