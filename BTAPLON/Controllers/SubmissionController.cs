using System;
using System.IO;
using System.Threading.Tasks;
using BTAPLON.Data;
using BTAPLON.Filters;
using BTAPLON.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTAPLON.Controllers
{
    [SessionAuthorize]
    public class SubmissionController : Controller
    {
        private readonly EduDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SubmissionController(EduDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // nộp bài
        private string RemoveUnicode(string text)
        {
            string[] signs = new string[] { "a", "a", "a", "a", "a", "a",
                                        "A","A","A","A","A","A",
                                        "e","e","e","e","E","E","E","E",
                                        "o","o","o","o","o","O","O","O","O","O",
                                        "u","u","u","u","U","U","U","U",
                                        "i","I",
                                        "d","D" };
            string[] accents = new string[]
            {
                "á","à","ả","ã","ạ","ă","ắ","ằ","ẳ","ẵ","ặ","â","ấ","ầ","ẩ","ẫ","ậ",
                "Á","À","Ả","Ã","Ạ","Ă","Ắ","Ằ","Ẳ","Ẵ","Ặ","Â","Ấ","Ầ","Ẩ","Ẫ","Ậ",
                "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
                "É","È","Ẻ","Ẽ","Ẹ","Ê","Ế","Ề","Ể","Ễ","Ệ",
                "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
                "Ó","Ò","Ỏ","Õ","Ọ","Ô","Ố","Ồ","Ổ","Ỗ","Ộ","Ơ","Ớ","Ờ","Ở","Ỡ","Ợ",
                "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
                "Ú","Ù","Ủ","Ũ","Ụ","Ư","Ứ","Ừ","Ử","Ữ","Ự",
                "í","ì","ỉ","ĩ","ị","Í","Ì","Ỉ","Ĩ","Ị",
                "đ","Đ"
            };

            for (int i = 0; i < accents.Length; i++)
            {
                text = text.Replace(accents[i], signs[i % signs.Length]);
            }

            return text.Replace(" ", "_");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentID, IFormFile fileUpload)
        {
            if (fileUpload == null || fileUpload.Length == 0)
            {
                return Content("Không có file hoặc file rỗng!");
            }

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var originalName = Path.GetFileNameWithoutExtension(fileUpload.FileName);
            var ext = Path.GetExtension(fileUpload.FileName);

            var safeName = RemoveUnicode(originalName);
            var fileName = $"{DateTime.Now.Ticks}_{safeName}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileUpload.CopyToAsync(stream);
            }

            var studentEmail = HttpContext.Session.GetString("UserEmail");
            var student = await _context.Users.FirstOrDefaultAsync(x => x.Email == studentEmail);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var submission = new Submission
            {
                AssignmentID = assignmentID,
                StudentID = student.UserID,
                FilePath = "/uploads/" + fileName,
                SubmittedAt = DateTime.Now,
                Score = null,
                Feedback = string.Empty
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Assignment", new { id = assignmentID });
        }

        [SessionAuthorize("Teacher", "Admin")]
        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var submission = await LoadSubmissionWithContext(id);
            if (submission == null)
            {
                return NotFound();
            }

            var permissionResult = EnsureTeacherPermission(submission);
            if (permissionResult != null)
            {
                return permissionResult;
            }

            return View(submission);
        }

        [SessionAuthorize("Teacher", "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int id, decimal? score, string? feedback)
        {
            var submission = await LoadSubmissionWithContext(id);
            if (submission == null)
            {
                return NotFound();
            }


            var permissionResult = EnsureTeacherPermission(submission);
            if (permissionResult != null)
            {
                return permissionResult;
            }

            submission.Score = score;
            submission.Feedback = feedback;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Assignment", new { id = submission.AssignmentID });
        }

        private async Task<Submission?> LoadSubmissionWithContext(int submissionId)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment!)
                    .ThenInclude(a => a.Class!)
                        .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(s => s.SubmissionID == submissionId);
        }

        private IActionResult? EnsureTeacherPermission(Submission submission)
        {
            {
                var role = HttpContext.Session.GetString("UserRole");

                if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var userId = HttpContext.Session.GetInt32("UserID");
                    var teacherId = submission.Assignment?.Class?.Course?.TeacherID;

                    if (!teacherId.HasValue || !userId.HasValue || teacherId.Value != userId.Value)
                    {
                        return new ForbidResult();
                    }
                }

                return null;
            }
        }
    

    // giáo viên chấm bài
    [HttpGet]
        public IActionResult Grade1(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            var submission = _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Class)
                .FirstOrDefault(s => s.SubmissionID == id);

            if (submission == null)
                return NotFound();

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Grade2(int id, decimal? score, string? feedback)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Account");

            var submission = _context.Submissions.FirstOrDefault(s => s.SubmissionID == id);
            if (submission == null)
                return NotFound();

            submission.Score = score;
            submission.Feedback = feedback;

            _context.Submissions.Update(submission);
            _context.SaveChanges();

            return RedirectToAction("Details", "Assignment", new { id = submission.AssignmentID });
        }

    }
}
