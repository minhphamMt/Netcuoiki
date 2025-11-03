using BTAPLON.Data;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
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
        public IActionResult Create()
        {
            ViewBag.ClassID = new SelectList(_context.Classes, "ClassID", "ClassCode");
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                .Include(x => x.Class)
                .Include(x => x.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefault(x => x.AssignmentID == id);

            if (a == null) return NotFound();

            return View(a);
        }
       

    }
}
