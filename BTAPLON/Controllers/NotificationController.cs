using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BTAPLON.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserDisplayName = HttpContext.Session.GetString("UserEmail") ?? "Ẩn danh";
            return View();
        }
    }
}