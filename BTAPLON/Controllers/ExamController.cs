using System.Linq;
using System.Text.Json;
using BTAPLON.Data;
using BTAPLON.Models;
using BTAPLON.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamChoice = BTAPLON.Models.Choice;
using ExamQuestion = BTAPLON.Models.Question;

namespace BTAPLON.Controllers
{
    public class ExamController : Controller
    {
        private readonly EduDbContext _context;

        public ExamController(EduDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserID");
        private string? CurrentUserRole => HttpContext.Session.GetString("UserRole");
        private bool IsTeacher => string.Equals(CurrentUserRole, "Teacher", StringComparison.OrdinalIgnoreCase);
        private bool IsAdmin => string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
        private bool IsStudent => string.Equals(CurrentUserRole, "Student", StringComparison.OrdinalIgnoreCase);

        private bool TeacherCanManageExam(Exam exam)
        {
            if (!IsTeacher || CurrentUserId is not int teacherId)
            {
                return false;
            }

            if (exam.CreatorID == teacherId)
            {
                return true;
            }

            if (exam.Class?.Course?.TeacherID == teacherId)
            {
                return true;
            }

            int? assignedTeacherId = _context.Classes
                .Where(c => c.ClassID == exam.ClassID)
                .Select(c => c.Course != null ? c.Course.TeacherID : null)
                .FirstOrDefault();

            return assignedTeacherId == teacherId;
        }

        private IActionResult? RequireLogin()
        {
            if (CurrentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        // TEACHER: list exams created by user
        public IActionResult Index()
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Available));
            }

            var examsQuery = _context.Exams
                .Include(e => e.Class)
                  .ThenInclude(c => c.Course)
                .Include(e => e.Submissions)
                .AsQueryable();

            if (IsTeacher && CurrentUserId is int teacherId)
            {
                examsQuery = examsQuery.Where(e => e.CreatorID == teacherId ||
                    (e.Class != null && e.Class.Course != null && e.Class.Course.TeacherID == teacherId));
            }

            var exams = examsQuery
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            return View(exams);
        }

        // TEACHER: create exam GET
        public IActionResult Create()
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            PopulateClassesDropDown();
            return View(new Exam());
        }

        // TEACHER: create exam POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Exam model)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                PopulateClassesDropDown(model.ClassID);
                return View(model);
            }

            model.CreatorID = CurrentUserId!.Value;
            model.CreatedAt = DateTime.Now;
            model.IsPublished = false;
            if (model.StartTime.HasValue && model.DurationMinutes > 0)
            {
                model.EndTime = model.StartTime.Value.AddMinutes(model.DurationMinutes);
            }

            _context.Exams.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Manage), new { id = model.ExamID });
        }

        // TEACHER: manage questions
        public IActionResult Manage(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var exam = _context.Exams
                    .Include(e => e.Class)
                     .ThenInclude(c => c.Course)
                    .Include(e => e.Questions)
                      .ThenInclude(q => q.Choices)
                    .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();

            if (IsTeacher && !TeacherCanManageExam(exam))
            {
                return Forbid();
            }

            ViewBag.QuestionForm = new ExamQuestionFormViewModel
            {
                ExamID = exam.ExamID,
                Points = 1,
                IsMultipleChoice = true
            };

            return View(exam);
        }

        // TEACHER: add question
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddQuestion(ExamQuestionFormViewModel model)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var exam = _context.Exams
                .Include(e => e.Class)
               .ThenInclude(c => c.Course)
                .Include(e => e.Questions)
                .FirstOrDefault(e => e.ExamID == model.ExamID);

            if (exam == null) return NotFound();
            if (IsTeacher && !TeacherCanManageExam(exam))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["QuestionError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Manage), new { id = model.ExamID });
            }

            var question = new ExamQuestion
            {
                ExamID = model.ExamID,
                Prompt = model.Prompt,
                IsMultipleChoice = model.IsMultipleChoice,
                Points = model.Points,
                DisplayOrder = exam.Questions?.Count + 1 ?? 1
            };

            _context.Questions.Add(question);
            _context.SaveChanges();

            var choices = model.Choices ?? new List<ChoiceOptionViewModel>();

            if (model.IsMultipleChoice || choices.Any(c => !string.IsNullOrWhiteSpace(c.Text)))
            {
                var validChoices = choices
                    .Where(c => !string.IsNullOrWhiteSpace(c.Text))
                    .Select(c => new ExamChoice
                    {
                        QuestionID = question.QuestionID,
                        Text = c.Text!.Trim(),
                        IsCorrect = c.IsCorrect
                    })
                    .ToList();

                if (!validChoices.Any())
                {
                    question.IsMultipleChoice = false;
                }
                else
                {
                    question.IsMultipleChoice = true;
                    if (!validChoices.Any(c => c.IsCorrect))
                    {
                        validChoices[0].IsCorrect = true;
                    }

                    foreach (var choice in validChoices)
                    {
                        _context.Choices.Add(choice);
                    }
                }

                _context.SaveChanges();
            }

            TempData["Message"] = "Đã thêm câu hỏi thành công.";
            return RedirectToAction(nameof(Manage), new { id = model.ExamID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteQuestion(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            var question = _context.Questions
               
                .FirstOrDefault(q => q.QuestionID == id);

            if (question == null) return NotFound();

            var exam = _context.Exams
              .Include(e => e.Class)
                  .ThenInclude(c => c.Course)
              .FirstOrDefault(e => e.ExamID == question.ExamID);

            if (exam == null)
            {
                return NotFound();
            }

            if (!IsAdmin && (!IsTeacher || !TeacherCanManageExam(exam)))
            {
                return Forbid();
            }

            _context.Questions.Remove(question);
            _context.SaveChanges();

            TempData["Message"] = "Đã xoá câu hỏi.";
            return RedirectToAction(nameof(Manage), new { id = question.ExamID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Publish(int id, DateTime? startTime, int durationMinutes)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            var exam = _context.Exams
                .Include(e => e.Class)
                  .ThenInclude(c => c.Course)
                .Include(e => e.Questions)
                .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();

            if (!IsAdmin && (!IsTeacher || !TeacherCanManageExam(exam)))
            {
                return Forbid();
            }

            if (exam.Questions == null || !exam.Questions.Any())
            {
                TempData["QuestionError"] = "Cần có ít nhất một câu hỏi trước khi mở bài thi.";
                return RedirectToAction(nameof(Manage), new { id });
            }

            if (durationMinutes > 0)
            {
                exam.DurationMinutes = durationMinutes;
            }

            if (startTime.HasValue)
            {
                exam.StartTime = startTime;
            }
            else if (!exam.StartTime.HasValue)
            {
                exam.StartTime = DateTime.Now;
            }

            exam.EndTime = exam.StartTime?.AddMinutes(exam.DurationMinutes);
            exam.IsPublished = true;
            exam.PublishedAt = DateTime.Now;

            _context.SaveChanges();

            TempData["Message"] = "Đã mở bài thi cho lớp.";
            return RedirectToAction(nameof(Manage), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Close(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            var exam = _context.Exams
                .Include(e => e.Class)
                  .ThenInclude(c => c.Course)
                .FirstOrDefault(e => e.ExamID == id);
            if (exam == null) return NotFound();

            if (!IsAdmin && (!IsTeacher || !TeacherCanManageExam(exam)))
            {
                return Forbid();
            }

            exam.IsPublished = false;
            _context.SaveChanges();

            TempData["Message"] = "Đã đóng bài thi.";
            return RedirectToAction(nameof(Manage), new { id });
        }

        // STUDENT: available exams
        public IActionResult Available()
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsStudent)
            {
                return RedirectToAction(nameof(Index));
            }

            var studentId = CurrentUserId!.Value;
            var classIds = _context.Enrollments
                .Where(e => e.StudentID == studentId)
                .Select(e => e.ClassID)
                .ToList();

            var now = DateTime.Now;
            var exams = _context.Exams
                .Include(e => e.Class)!
                .ThenInclude(c => c.Course)
                .Where(e => e.IsPublished && classIds.Contains(e.ClassID) &&
                            (!e.StartTime.HasValue || e.StartTime <= now) &&
                            (!e.EndTime.HasValue || e.EndTime >= now))
                .OrderBy(e => e.StartTime)
                .ToList();

            var submissions = _context.ExamSubmissions
                .Where(s => s.StudentID == studentId)
                .ToList();

            ViewBag.Submissions = submissions;
            return View(exams);
        }

        // STUDENT: take exam GET
        public IActionResult Take(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsStudent)
            {
                return RedirectToAction(nameof(Index));
            }

            var exam = _context.Exams
                .Include(e => e.Class)
                   .ThenInclude(c => c.Course)
                .Include(e => e.Questions!)
                 .ThenInclude(q => q.Choices)
                .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();
            if (!exam.IsPublished)
            {
                return RedirectToAction(nameof(Available));
            }

            if (exam.Questions == null || !exam.Questions.Any())
            {
                TempData["QuestionError"] = "Đề thi chưa có câu hỏi.";
                return RedirectToAction(nameof(Available));
            }

            var now = DateTime.Now;
            if ((exam.StartTime.HasValue && exam.StartTime.Value > now) ||
                (exam.EndTime.HasValue && exam.EndTime.Value < now))
            {
                TempData["QuestionError"] = "Bài thi chưa mở hoặc đã kết thúc.";
                return RedirectToAction(nameof(Available));
            }

            var studentId = CurrentUserId!.Value;
            var enrolled = _context.Enrollments.Any(e => e.StudentID == studentId && e.ClassID == exam.ClassID);
            if (!enrolled) return Forbid();

            var submission = _context.ExamSubmissions
                .FirstOrDefault(s => s.ExamID == id && s.StudentID == studentId);

            if (submission != null && submission.IsFinalized)
            {
                return RedirectToAction(nameof(Result), new { submissionId = submission.ExamSubmissionID });
            }

            if (submission == null)
            {
                submission = new ExamSubmission
                {
                    ExamID = id,
                    StudentID = studentId,
                    StartedAt = DateTime.Now,
                    IsFinalized = false
                };
                _context.ExamSubmissions.Add(submission);
                _context.SaveChanges();
            }

            var answers = new Dictionary<int, ExamSubmissionAnswer>();
            if (!string.IsNullOrWhiteSpace(submission.AnswersJson))
            {
                try
                {
                    var stored = JsonSerializer.Deserialize<List<ExamSubmissionAnswer>>(submission.AnswersJson);
                    if (stored != null)
                    {
                        answers = stored.ToDictionary(a => a.QuestionId, a => a);
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            var timeLimit = submission.StartedAt.AddMinutes(exam.DurationMinutes);
            var remaining = Math.Max(0, (int)(timeLimit - DateTime.Now).TotalSeconds);
            if (remaining <= 0)
            {
                if (exam.Questions != null)
                {
                    double earned = 0;
                    foreach (ExamQuestion question in exam.Questions)
                    {
                        if (!question.IsMultipleChoice) continue;
                        if (answers.TryGetValue(question.QuestionID, out var record) && record.SelectedChoiceId.HasValue)
                        {
                            var selected = question.Choices?.FirstOrDefault(c => c.ChoiceID == record.SelectedChoiceId);
                            if (selected != null && selected.IsCorrect)
                            {
                                earned += question.Points;
                            }
                        }
                    }

                    submission.Score = Math.Round(earned, 2);
                    submission.AnswersJson = JsonSerializer.Serialize(answers.Values.ToList());
                }

                submission.IsFinalized = true;
                submission.SubmittedAt = DateTime.Now;
                _context.SaveChanges();

                TempData["QuestionError"] = "Thời gian làm bài đã hết. Bài đã được lưu tự động.";
                return RedirectToAction(nameof(Result), new { submissionId = submission.ExamSubmissionID });
            }

            var vm = new ExamTakeViewModel
            {
                Exam = exam,
                Submission = submission,
                Answers = answers,
                RemainingSeconds = remaining
            };

            return View(vm);
        }

        // STUDENT: submit exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(int id, Dictionary<int, string> answers)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsStudent)
            {
                return RedirectToAction(nameof(Index));
            }

            answers ??= new Dictionary<int, string>();

            var exam = _context.Exams
                .Include(e => e.Questions!)
                    .ThenInclude(q => q.Choices)
                .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();

            var studentId = CurrentUserId!.Value;
            var submission = _context.ExamSubmissions
                .FirstOrDefault(s => s.ExamID == id && s.StudentID == studentId);

            if (submission == null)
            {
                return RedirectToAction(nameof(Take), new { id });
            }

            if (submission.IsFinalized)
            {
                return RedirectToAction(nameof(Result), new { submissionId = submission.ExamSubmissionID });
            }

            var records = new List<ExamSubmissionAnswer>();
            double totalScore = 0;
            double earnedScore = 0;

            foreach (ExamQuestion question in exam.Questions!.OrderBy(q => q.DisplayOrder))
            {
                answers.TryGetValue(question.QuestionID, out var rawValue);
                var record = new ExamSubmissionAnswer
                {
                    QuestionId = question.QuestionID
                };

                if (question.IsMultipleChoice)
                {
                    if (int.TryParse(rawValue, out var choiceId))
                    {
                        record.SelectedChoiceId = choiceId;
                        var selected = question.Choices?.FirstOrDefault(c => c.ChoiceID == choiceId);
                        if (selected != null && selected.IsCorrect)
                        {
                            earnedScore += question.Points;
                        }
                    }
                }
                else
                {
                    record.AnswerText = rawValue;
                }

                records.Add(record);
                totalScore += question.Points;
            }

            submission.AnswersJson = JsonSerializer.Serialize(records);
            submission.SubmittedAt = DateTime.Now;
            submission.IsFinalized = true;
            submission.Score = totalScore > 0 ? Math.Round(earnedScore, 2) : earnedScore;

            _context.SaveChanges();

            return RedirectToAction(nameof(Result), new { submissionId = submission.ExamSubmissionID });
        }

        // STUDENT: submission result
        public IActionResult Result(int submissionId)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            var submission = _context.ExamSubmissions
         .Include(s => s.Exam)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                .FirstOrDefault(s => s.ExamSubmissionID == submissionId);

            if (submission == null) return NotFound();

            if (submission.Exam == null)
            {
                return NotFound();
            }

            if (submission.StudentID != CurrentUserId)
            {
                if (IsTeacher)
                {
                    if (!TeacherCanManageExam(submission.Exam))
                    {
                        return Forbid();
                    }
                }
                else if (!IsAdmin)
                {
                    return Forbid();
                }
            }

            var answers = new List<ExamSubmissionAnswer>();
            if (!string.IsNullOrWhiteSpace(submission.AnswersJson))
            {
                try
                {
                    var stored = JsonSerializer.Deserialize<List<ExamSubmissionAnswer>>(submission.AnswersJson);
                    if (stored != null)
                    {
                        answers = stored;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            var vm = new ExamReviewSubmissionViewModel
            {
                Exam = submission.Exam,
                Submission = submission,
                Answers = answers
            };

            return View(vm);
        }

        // TEACHER: review submissions list
        public IActionResult Review(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var exam = _context.Exams
                .Include(e => e.Class)
                .ThenInclude(c => c.Course)
                .Include(e => e.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();

            if (IsTeacher && !TeacherCanManageExam(exam))
            {
                return Forbid();
            }

            return View(exam);
        }

        // TEACHER: grade single submission
        public IActionResult GradeSubmission(int submissionId)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var submission = _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Choices)
                .Include(s => s.Student)
                .FirstOrDefault(s => s.ExamSubmissionID == submissionId);

            if (submission == null) return NotFound();

            if (submission.Exam == null)
            {
                return NotFound();
            }

            if (IsTeacher && !TeacherCanManageExam(submission.Exam))
            {
                return Forbid();
            }

            var answers = new List<ExamSubmissionAnswer>();
            if (!string.IsNullOrWhiteSpace(submission.AnswersJson))
            {
                try
                {
                    var stored = JsonSerializer.Deserialize<List<ExamSubmissionAnswer>>(submission.AnswersJson);
                    if (stored != null)
                    {
                        answers = stored;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var vm = new ExamReviewSubmissionViewModel
            {
                Exam = submission.Exam,
                Submission = submission,
                Answers = answers
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GradeSubmission(int submissionId, double? score, string? feedback)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var submission = _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .FirstOrDefault(s => s.ExamSubmissionID == submissionId);

            if (submission == null) return NotFound();

            if (submission.Exam == null)
            {
                return NotFound();
            }

            if (IsTeacher && !TeacherCanManageExam(submission.Exam))
            {
                return Forbid();
            }

            submission.Score = score;
            submission.TeacherFeedback = feedback;
            if (!submission.IsFinalized)
            {
                submission.IsFinalized = true;
                submission.SubmittedAt = submission.SubmittedAt ?? DateTime.Now;
            }

            _context.SaveChanges();

            TempData["Message"] = "Đã cập nhật điểm.";
            return RedirectToAction(nameof(Review), new { id = submission.ExamID });
        }

        // TEACHER: statistics
        public IActionResult Statistics(int id)
        {
            var redirect = RequireLogin();
            if (redirect != null) return redirect;

            if (!IsTeacher && !IsAdmin)
            {
                return RedirectToAction(nameof(Index));
            }

            var exam = _context.Exams
                .Include(e => e.Class)
                .ThenInclude(c => c.Course)
                .Include(e => e.Submissions!)
                .ThenInclude(s => s.Student)
                .FirstOrDefault(e => e.ExamID == id);

            if (exam == null) return NotFound();

            if (IsTeacher && !TeacherCanManageExam(exam))
            {
                return Forbid();
            }

            var totalStudents = _context.Enrollments.Count(e => e.ClassID == exam.ClassID);
            var finalized = exam.Submissions?.Where(s => s.IsFinalized && s.Score.HasValue).ToList() ?? new List<ExamSubmission>();

            double average = finalized.Any() ? finalized.Average(s => s.Score!.Value) : 0;
            double highest = finalized.Any() ? finalized.Max(s => s.Score!.Value) : 0;
            double lowest = finalized.Any() ? finalized.Min(s => s.Score!.Value) : 0;

            var distribution = new Dictionary<string, int>
       {
                { ">= 9", finalized.Count(s => s.Score >= 9) },
                { "8 - 8.99", finalized.Count(s => s.Score >= 8 && s.Score < 9) },
                { "7 - 7.99", finalized.Count(s => s.Score >= 7 && s.Score < 8) },
                { "5 - 6.99", finalized.Count(s => s.Score >= 5 && s.Score < 7) },
                { "< 5", finalized.Count(s => s.Score < 5) }
            };

            var vm = new ExamStatisticsViewModel
            {
                Exam = exam,
                TotalStudents = totalStudents,
                TotalSubmissions = exam.Submissions?.Count ?? 0,
                AverageScore = Math.Round(average, 2),
                HighestScore = Math.Round(highest, 2),
                LowestScore = Math.Round(lowest, 2),
                GradeDistribution = distribution
            };

            return View(vm);
        }

        private void PopulateClassesDropDown(int? selectedId = null)
        {
            var classesQuery = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (IsTeacher)
            {
                classesQuery = classesQuery.Where(c => c.Course!.TeacherID == CurrentUserId);
            }

            var classes = classesQuery
                .Select(c => new
                {
                    c.ClassID,
                    Name = $"{c.ClassCode} - {c.Course!.CourseName}"
                })
                .ToList();

            ViewBag.ClassID = new SelectList(classes, "ClassID", "Name", selectedId);
        }
    }
}