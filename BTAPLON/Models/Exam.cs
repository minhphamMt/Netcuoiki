using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BTAPLON.Models
{
    public class Exam
    {
        public int ExamID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int ClassID { get; set; }

        public int CreatorID { get; set; }

        [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút")]
        public int DurationMinutes { get; set; } = 60;

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsPublished { get; set; }

        public bool PreventBackNavigation { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PublishedAt { get; set; }

        [ValidateNever]
        public Class? Class { get; set; }

        [ValidateNever]
        public User? Creator { get; set; }

        [ValidateNever]
        public ICollection<Question>? Questions { get; set; }

        [ValidateNever]
        public ICollection<ExamSubmission>? Submissions { get; set; }
    }
}