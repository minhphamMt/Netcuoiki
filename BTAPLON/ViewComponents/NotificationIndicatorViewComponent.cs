using System;
using System.Linq;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.ViewComponents
{
    public class NotificationIndicatorViewComponent : ViewComponent
    {
        private readonly EduDbContext _context;

        public NotificationIndicatorViewComponent(EduDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var session = HttpContext?.Session;
            var userId = session?.GetInt32("UserID");
            var role = session?.GetString("UserRole");

            var model = new NotificationIndicatorViewModel();

            if (userId == null || string.IsNullOrEmpty(role))
            {
                return View(model);
            }

            var query = _context.Notifications
                .Include(n => n.Class)
                    .ThenInclude(c => c.Course)
                .Include(n => n.Class)
                    .ThenInclude(c => c.Enrollments)
                .AsNoTracking()
                .AsQueryable();

            var isTeacher = string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase);
            var isStudent = string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (isTeacher)
            {
                query = query.Where(n => n.Class != null && n.Class.Course != null && n.Class.Course.TeacherID == userId.Value);
            }
            else if (isStudent)
            {
                query = query.Where(n => n.Class != null && n.Class.Enrollments.Any(e => e.StudentID == userId.Value));
            }
            else if (!isAdmin)
            {
                query = query.Where(n => n.CreatedByID == userId.Value);
            }

            var unreadCount = await query
                .Where(n => !_context.NotificationReceipts.Any(r => r.NotificationID == n.NotificationID && r.UserID == userId.Value))
                .CountAsync();

            model.UnreadCount = unreadCount;

            return View(model);
        }
    }
}