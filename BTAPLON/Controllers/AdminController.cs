using BTAPLON.Data;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult Users()
        {
            var users = _context.Users.ToList();
            return View(users);
        }
        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(User user)
        {
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
            user.PasswordHash = model.PasswordHash;
            user.Role = model.Role;

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
