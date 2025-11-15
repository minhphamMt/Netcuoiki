using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    public class NotificationController : Controller
    {
        private readonly EduDbContext _context;

        public NotificationController(EduDbContext context)
        {
            _context = context;
        }

        // ----------------------------
        // INDEX
        // ----------------------------
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var viewModel = await BuildIndexViewModelAsync(userId.Value, role, null, searchTerm);
            return View(viewModel);
        }

        // ----------------------------
        // CREATE
        // ----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> Create([Bind(Prefix = "Form")] NotificationFormInput form)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Validate form
            if (string.IsNullOrWhiteSpace(form.Title))
                ModelState.AddModelError("Form.Title", "Tiêu đề không được để trống hoặc chỉ có khoảng trắng.");
            if (string.IsNullOrWhiteSpace(form.Content))
                ModelState.AddModelError("Form.Content", "Nội dung không được để trống hoặc chỉ có khoảng trắng.");

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildIndexViewModelAsync(userId.Value, role, form, null);
                return View("Index", invalidModel);
            }

            // Kiểm tra quyền giáo viên
            var teacherClass = await _context.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c =>
                    c.ClassID == form.ClassId &&
                    c.Course != null &&
                    c.Course.TeacherID == userId.Value);

            if (teacherClass == null)
            {
                ModelState.AddModelError("Form.ClassId", "Bạn chỉ có thể gửi thông báo cho lớp mà bạn phụ trách.");
                var invalidModel = await BuildIndexViewModelAsync(userId.Value, role, form, null);
                return View("Index", invalidModel);
            }

            // Tạo thông báo
            var notification = new Notification
            {
                ClassID = teacherClass.ClassID,
                Title = form.Title!.Trim(),
                Content = form.Content!.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByID = userId.Value
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var courseName = teacherClass.Course?.CourseName ?? "Lớp học";
            var classCode = teacherClass.ClassCode;
            TempData["SuccessMessage"] = classCode == null
                ? $"Đã gửi thông báo tới lớp {courseName}."
                : $"Đã gửi thông báo tới lớp {courseName} ({classCode}).";

            return RedirectToAction(nameof(Index));
        }

        // ----------------------------
        // EDIT (GET)
        // ----------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var notification = await _context.Notifications
                .Include(n => n.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(n => n.NotificationID == id);

            if (notification == null ||
                notification.Class?.Course == null ||
                notification.Class.Course.TeacherID != userId.Value)
            {
                return NotFound();
            }

            var viewModel = await BuildEditViewModelAsync(notification, userId.Value);
            return View(viewModel);
        }

        // ----------------------------
        // EDIT (POST)
        // ----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Form")] NotificationFormInput form)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var notification = await _context.Notifications
                .Include(n => n.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(n => n.NotificationID == id);

            if (notification == null ||
                notification.Class?.Course == null ||
                notification.Class.Course.TeacherID != userId.Value)
            {
                return NotFound();
            }

            // Validate form
            if (string.IsNullOrWhiteSpace(form.Title))
                ModelState.AddModelError("Form.Title", "Tiêu đề không được để trống hoặc chỉ có khoảng trắng.");
            if (string.IsNullOrWhiteSpace(form.Content))
                ModelState.AddModelError("Form.Content", "Nội dung không được để trống hoặc chỉ có khoảng trắng.");

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildEditViewModelAsync(notification, userId.Value, form);
                return View(invalidViewModel);
            }

            // Kiểm tra lớp mục tiêu
            var targetClass = await _context.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c =>
                    c.ClassID == form.ClassId &&
                    c.Course != null &&
                    c.Course.TeacherID == userId.Value);

            if (targetClass == null)
            {
                ModelState.AddModelError("Form.ClassId", "Bạn chỉ có thể chọn lớp mà bạn phụ trách.");
                var invalidViewModel = await BuildEditViewModelAsync(notification, userId.Value, form);
                return View(invalidViewModel);
            }

            // Cập nhật thông báo
            notification.ClassID = targetClass.ClassID;
            notification.Title = form.Title!.Trim();
            notification.Content = form.Content!.Trim();

            await _context.SaveChangesAsync();

            var courseName = targetClass.Course?.CourseName ?? "Lớp học";
            var classCode = targetClass.ClassCode;
            TempData["SuccessMessage"] = classCode == null
                ? $"Đã cập nhật thông báo cho lớp {courseName}."
                : $"Đã cập nhật thông báo cho lớp {courseName} ({classCode}).";

            return RedirectToAction(nameof(Index));
        }

        // ----------------------------
        // DELETE
        // ----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var notification = await _context.Notifications
                .Include(n => n.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(n => n.NotificationID == id);

            if (notification == null ||
                notification.Class?.Course == null ||
                notification.Class.Course.TeacherID != userId.Value)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            var courseName = notification.Class.Course?.CourseName ?? "Lớp học";
            var classCode = notification.Class.ClassCode;
            TempData["SuccessMessage"] = classCode == null
                ? $"Đã xóa thông báo của lớp {courseName}."
                : $"Đã xóa thông báo của lớp {courseName} ({classCode}).";

            return RedirectToAction(nameof(Index));
        }

        // ----------------------------
        // PRIVATE HELPERS
        // ----------------------------

        private async Task<NotificationIndexViewModel> BuildIndexViewModelAsync(int userId, string? role, NotificationFormInput? form, string? searchTerm = null)
        {
            var isTeacher = string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase);
            var isStudent = string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            var query = _context.Notifications
                .Include(n => n.Class)
                    .ThenInclude(c => c.Course)
                .Include(n => n.Class)
                    .ThenInclude(c => c.Enrollments)
                .Include(n => n.CreatedBy)
                .AsQueryable();

            if (isTeacher)
            {
                query = query.Where(n => n.Class != null && n.Class.Course != null && n.Class.Course.TeacherID == userId);
            }
            else if (isStudent)
            {
                query = query.Where(n => n.Class != null && n.Class.Enrollments.Any(e => e.StudentID == userId));
            }
            else if (!isAdmin)
            {
                query = query.Where(n => n.CreatedByID == userId);
            }

            var trimmedSearch = searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var normalized = trimmedSearch.ToLower();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(normalized) ||
                    (n.Content != null && n.Content.ToLower().Contains(normalized)) ||
                    (n.Class != null && (
                        (!string.IsNullOrEmpty(n.Class.ClassCode) && n.Class.ClassCode.ToLower().Contains(normalized)) ||
                        (n.Class.Course != null && n.Class.Course.CourseName != null && n.Class.Course.CourseName.ToLower().Contains(normalized))
                    )) ||
                    (n.CreatedBy != null && (
                        (n.CreatedBy.FullName != null && n.CreatedBy.FullName.ToLower().Contains(normalized)) ||
                        (n.CreatedBy.Email != null && n.CreatedBy.Email.ToLower().Contains(normalized))
                    )) ||
                    n.NotificationID.ToString().Contains(normalized));
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDisplayViewModel
                {
                    Id = n.NotificationID,
                    ClassId = n.ClassID,
                    ClassName = n.Class!.Course != null ? n.Class.Course.CourseName : "Lớp học",
                    ClassCode = n.Class.ClassCode,
                    Title = n.Title,
                    Content = n.Content,
                    Author = !string.IsNullOrEmpty(n.CreatedBy!.FullName)
                        ? n.CreatedBy.FullName
                        : (n.CreatedBy.Email ?? "Giáo viên"),
                    CreatedAt = n.CreatedAt,
                    CanManage = isTeacher && n.CreatedByID == userId
                })
                .ToListAsync();

            IList<SelectListItem> teacherClasses = new List<SelectListItem>();
            if (isTeacher)
            {
                teacherClasses = await _context.Classes
                    .Include(c => c.Course)
                    .Where(c => c.Course != null && c.Course.TeacherID == userId)
                    .OrderBy(c => c.Course!.CourseName)
                    .ThenBy(c => c.ClassCode)
                    .Select(c => new SelectListItem
                    {
                        Value = c.ClassID.ToString(),
                        Text = c.Course!.CourseName +
                               (string.IsNullOrWhiteSpace(c.ClassCode) ? string.Empty : $" ({c.ClassCode})")
                    })
                    .ToListAsync();
            }

            return new NotificationIndexViewModel
            {
                CanCreate = isTeacher,
                TeacherClasses = teacherClasses,
                Notifications = notifications,
                Form = form ?? new NotificationFormInput(),
                SearchTerm = trimmedSearch
            };
        }

        private async Task<NotificationEditViewModel> BuildEditViewModelAsync(Notification notification, int userId, NotificationFormInput? form = null)
        {
            var selectedClassId = form?.ClassId ?? notification.ClassID;

            var teacherClasses = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.Course != null && c.Course.TeacherID == userId)
                .OrderBy(c => c.Course!.CourseName)
                .ThenBy(c => c.ClassCode)
                .Select(c => new SelectListItem
                {
                    Value = c.ClassID.ToString(),
                    Text = c.Course!.CourseName +
                           (string.IsNullOrWhiteSpace(c.ClassCode) ? string.Empty : $" ({c.ClassCode})"),
                    Selected = selectedClassId == c.ClassID
                })
                .ToListAsync();

            var selectedClassLabel = teacherClasses.FirstOrDefault(c => c.Value == selectedClassId.ToString());
            var heading = selectedClassLabel != null
                ? $"Chỉnh sửa thông báo - {selectedClassLabel.Text}"
                : $"Chỉnh sửa thông báo - {(notification.Class?.Course?.CourseName ?? "Lớp học")}" +
                  (notification.Class?.ClassCode != null ? $" ({notification.Class.ClassCode})" : "");

            return new NotificationEditViewModel
            {
                Id = notification.NotificationID,
                Heading = heading,
                TeacherClasses = teacherClasses,
                Form = form ?? new NotificationFormInput
                {
                    ClassId = notification.ClassID,
                    Title = notification.Title,
                    Content = notification.Content
                }
            };
        }
    }
}
