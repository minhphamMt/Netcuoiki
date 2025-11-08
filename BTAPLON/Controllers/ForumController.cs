using System;
using BTAPLON.Data;
using BTAPLON.Models;
using BTAPLON.Models.Forum;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    public class ForumController : Controller
    {
        private readonly EduDbContext _context;
        private readonly ILogger<ForumController> _logger;

        public ForumController(EduDbContext context, ILogger<ForumController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? courseId, int? classId)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var loadResult = await LoadContextAsync(courseId, classId);
            if (!loadResult.success)
            {
                return NotFound();
            }

            var course = loadResult.course;
            var cls = loadResult.cls;

            if (!await HasForumAccessAsync(userId, role, course, cls))
            {
                return Forbid();
            }

            var threadQuery = _context.DiscussionThreads
                .Include(t => t.CreatedBy)
                .Include(t => t.Posts)
                .AsQueryable();

            var questionQuery = _context.ForumQuestions
                .Include(q => q.Student)
                .Include(q => q.Answers)
                .AsQueryable();

            if (cls != null)
            {
                threadQuery = threadQuery.Where(t => t.ClassID == cls.ClassID);
                questionQuery = questionQuery.Where(q => q.ClassID == cls.ClassID);
            }
            else if (course != null)
            {
                threadQuery = threadQuery.Where(t => t.CourseID == course.CourseID && t.ClassID == null);
                questionQuery = questionQuery.Where(q => q.CourseID == course.CourseID && q.ClassID == null);
            }

            var viewModel = new ForumIndexViewModel
            {
                Course = course,
                Class = cls,
                Threads = await threadQuery
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(),
                Questions = await questionQuery
                    .OrderByDescending(q => q.CreatedAt)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateThread(int? courseId, int? classId)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var loadResult = await LoadContextAsync(courseId, classId);
            if (!loadResult.success)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, loadResult.course, loadResult.cls))
            {
                return Forbid();
            }

            ViewBag.Course = loadResult.course;
            ViewBag.Class = loadResult.cls;

            return View(new ThreadCreateViewModel
            {
                CourseID = courseId,
                ClassID = classId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateThread(ThreadCreateViewModel model)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!model.CourseID.HasValue && !model.ClassID.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn khóa học hoặc lớp học cho chủ đề.");
            }

            var loadResult = await LoadContextAsync(model.CourseID, model.ClassID);
            if (!loadResult.success)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, loadResult.course, loadResult.cls))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Course = loadResult.course;
                ViewBag.Class = loadResult.cls;
                return View(model);
            }

            var thread = new DiscussionThread
            {
                Title = model.Title,
                CourseID = model.CourseID,
                ClassID = model.ClassID,
                CreatedByID = userId,
                CreatedAt = DateTime.UtcNow,
                Description = BuildSummary(model.Content)
            };

            _context.DiscussionThreads.Add(thread);
            await _context.SaveChangesAsync();

            var firstPost = new Post
            {
                DiscussionThreadID = thread.DiscussionThreadID,
                AuthorID = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                IsPinned = true
            };

            _context.Posts.Add(firstPost);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Thread), new { id = thread.DiscussionThreadID });
        }

        [HttpGet]
        public async Task<IActionResult> Thread(int id)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var thread = await _context.DiscussionThreads
                .Include(t => t.Course)
                .Include(t => t.Class)
                .Include(t => t.CreatedBy)
                .Include(t => t.Posts)
                    .ThenInclude(p => p.Author)
                .FirstOrDefaultAsync(t => t.DiscussionThreadID == id);

            if (thread == null)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, thread.Course, thread.Class))
            {
                return Forbid();
            }

            return View(thread);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReply(PostCreateViewModel model)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var thread = await _context.DiscussionThreads
                .Include(t => t.Course)
                .Include(t => t.Class)
                .FirstOrDefaultAsync(t => t.DiscussionThreadID == model.DiscussionThreadID);
            if (thread == null)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, thread.Course, thread.Class))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["ReplyError"] = "Nội dung trả lời không hợp lệ.";
                return RedirectToAction(nameof(Thread), new { id = model.DiscussionThreadID });
            }

            if (thread.IsLocked)
            {
                TempData["ReplyError"] = "Chủ đề đã bị khóa.";
                return RedirectToAction(nameof(Thread), new { id = model.DiscussionThreadID });
            }

            var post = new Post
            {
                DiscussionThreadID = model.DiscussionThreadID,
                AuthorID = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Thread), new { id = model.DiscussionThreadID });
        }

        [HttpGet]
        public async Task<IActionResult> AskQuestion(int? courseId, int? classId)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var loadResult = await LoadContextAsync(courseId, classId);
            if (!loadResult.success)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, loadResult.course, loadResult.cls))
            {
                return Forbid();
            }

            ViewBag.Course = loadResult.course;
            ViewBag.Class = loadResult.cls;

            return View(new QuestionCreateViewModel
            {
                CourseID = courseId,
                ClassID = classId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskQuestion(QuestionCreateViewModel model)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!model.CourseID.HasValue && !model.ClassID.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn khóa học hoặc lớp học cho câu hỏi.");
            }

            var loadResult = await LoadContextAsync(model.CourseID, model.ClassID);
            if (!loadResult.success)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, loadResult.course, loadResult.cls))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Course = loadResult.course;
                ViewBag.Class = loadResult.cls;
                return View(model);
            }

            var question = new ForumQuestion
            {
                Title = model.Title,
                Body = model.Body,
                CourseID = model.CourseID,
                ClassID = model.ClassID,
                StudentID = userId,
                CreatedAt = DateTime.UtcNow
            };

            var teacher = await FindTeacherAsync(loadResult.course, loadResult.cls);
            if (teacher != null)
            {
                question.TeacherNotifiedAt = DateTime.UtcNow;
                _logger.LogInformation("Thông báo giáo viên {TeacherEmail} về câu hỏi mới: {Title}", teacher.Email, question.Title);
            }

            _context.ForumQuestions.Add(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Question), new { id = question.QuestionID });
        }

        [HttpGet]
        public async Task<IActionResult> Question(int id)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var question = await _context.ForumQuestions
                .Include(q => q.Course)
                .Include(q => q.Class)
                .Include(q => q.Student)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Author)
                .FirstOrDefaultAsync(q => q.QuestionID == id);

            if (question == null)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, question.Course, question.Class))
            {
                return Forbid();
            }

            var teacherId = await GetTeacherIdAsync(question.CourseID, question.ClassID);
            ViewBag.CanMarkAnswers = userId == question.StudentID ||
                                     (teacherId.HasValue && teacherId.Value == userId);
            
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAnswer(AnswerCreateViewModel model)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var question = await _context.ForumQuestions
                .Include(q => q.Answers)
                .Include(q => q.Course)
                .Include(q => q.Class)
                .FirstOrDefaultAsync(q => q.QuestionID == model.QuestionID);

            if (question == null)
            {
                return NotFound();
            }

            if (!await HasForumAccessAsync(userId, role, question.Course, question.Class))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["AnswerError"] = "Nội dung câu trả lời không hợp lệ.";
                return RedirectToAction(nameof(Question), new { id = model.QuestionID });
            }

            var answer = new Answer
            {
                QuestionID = model.QuestionID,
                AuthorID = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Question), new { id = model.QuestionID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAnswer(int id)
        {
            if (!EnsureAuthenticated(out var userId, out var role))
            {
                return RedirectToAction("Login", "Account");
            }

            var answer = await _context.Answers
                .Include(a => a.Question)
                    .ThenInclude(q => q.Answers)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Student)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Class)
                        .ThenInclude(c => c.Course)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Course)
                        .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(a => a.AnswerID == id);

            if (answer == null)
            {
                return NotFound();
            }

            var question = answer.Question!;

            if (!await HasForumAccessAsync(userId, role, question.Course, question.Class))
            {
                return Forbid();
            }

            var ownerId = question.StudentID;
            var teacherId = await GetTeacherIdAsync(question.CourseID, question.ClassID);

            if (userId != ownerId && userId != teacherId)
            {
                TempData["AnswerError"] = "Bạn không có quyền đánh dấu câu trả lời.";
                return RedirectToAction(nameof(Question), new { id = question.QuestionID });
            }

            foreach (var other in question.Answers)
            {
                other.IsAccepted = false;
            }

            answer.IsAccepted = true;
            question.IsResolved = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Question), new { id = question.QuestionID });
        }

        private bool EnsureAuthenticated(out int userId, out string? role)
        {
            userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            return userId != 0;
        }

        private async Task<bool> HasForumAccessAsync(int userId, string? role, Course? course, Class? cls)
        {
            if (userId == 0 || string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (cls != null)
            {
                if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var teachesClass = await _context.Classes
                        .Where(c => c.ClassID == cls.ClassID)
                        .AnyAsync(c => c.Course != null && c.Course.TeacherID == userId);

                    if (teachesClass)
                    {
                        return true;
                    }
                }

                if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    var enrolled = await _context.Enrollments
                        .AnyAsync(e => e.ClassID == cls.ClassID && e.StudentID == userId);

                    if (enrolled)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (course != null)
            {
                if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var ownsCourse = await _context.Courses
                        .AnyAsync(c => c.CourseID == course.CourseID && c.TeacherID == userId);

                    if (ownsCourse)
                    {
                        return true;
                    }
                }

                if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    var hasEnrollment = await _context.Enrollments
                        .AnyAsync(e => e.StudentID == userId && e.Class != null && e.Class.CourseID == course.CourseID);

                    if (hasEnrollment)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task<(bool success, Course? course, Class? cls)> LoadContextAsync(int? courseId, int? classId)
        {
            if (classId.HasValue)
            {
                var cls = await _context.Classes
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ClassID == classId.Value);

                return cls == null ? (false, null, null) : (true, cls.Course, cls);
            }

            if (courseId.HasValue)
            {
                var course = await _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefaultAsync(c => c.CourseID == courseId.Value);

                return course == null ? (false, null, null) : (true, course, null);
            }

            return (false, null, null);
        }

        private string? BuildSummary(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var trimmed = content.Trim();
            return trimmed.Length <= 180 ? trimmed : trimmed.Substring(0, 177) + "...";
        }

        private async Task<User?> FindTeacherAsync(Course? course, Class? cls)
        {
            if (cls != null)
            {
                return await _context.Classes
                    .Where(c => c.ClassID == cls.ClassID)
                    .Select(c => c.Course!.Teacher)
                    .FirstOrDefaultAsync();
            }

            if (course != null)
            {
                return await _context.Courses
                    .Where(c => c.CourseID == course.CourseID)
                    .Select(c => c.Teacher)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        private async Task<int?> GetTeacherIdAsync(int? courseId, int? classId)
        {
            if (classId.HasValue)
            {
                return await _context.Classes
                    .Where(c => c.ClassID == classId.Value)
                    .Select(c => c.Course!.TeacherID)
                    .FirstOrDefaultAsync();
            }

            if (courseId.HasValue)
            {
                return await _context.Courses
                    .Where(c => c.CourseID == courseId.Value)
                    .Select(c => c.TeacherID)
                    .FirstOrDefaultAsync();
            }

            return null;
        }
    }
}