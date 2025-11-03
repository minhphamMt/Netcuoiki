using BTAPLON.Data;
using BTAPLON.Models;
using Microsoft.AspNetCore.Mvc;

public class SubmissionController : Controller
{
    public IActionResult Index1()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            return RedirectToAction("Login", "Account");

        return View();
    }

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
    public async Task<IActionResult> Submit(int assignmentID, IFormFile fileUpload)
    {
        if (fileUpload == null || fileUpload.Length == 0)
            return Content("Không có file hoặc file rỗng!");

        // Tạo folder nếu chưa có
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        // tạo tên file không trùng
        var originalName = Path.GetFileNameWithoutExtension(fileUpload.FileName);
        var ext = Path.GetExtension(fileUpload.FileName);

        // remove unicode
        var safeName = RemoveUnicode(originalName);
        var fileName = $"{DateTime.Now.Ticks}_{safeName}{ext}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileUpload.CopyToAsync(stream);
        }

        // user đang login
        var studentEmail = HttpContext.Session.GetString("UserEmail");
        var student = _context.Users.FirstOrDefault(x => x.Email == studentEmail);

        if (student == null)
            return Content("Không tìm thấy user trong session");

        var submission = new Submission
        {
            AssignmentID = assignmentID,
            StudentID = student.UserID,
            FilePath = "/uploads/" + fileName,
            SubmittedAt = DateTime.Now,
            Score = null,      // optional
            Feedback = ""      // <= thêm dòng này
        };

        _context.Submissions.Add(submission);
        _context.SaveChanges();

        return RedirectToAction("Details", "Assignment", new { id = assignmentID });
    }



}
