using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models.Forum
{
    public class ForumQuestion
    {
        [Key]
        public int QuestionID { get; set; }

        public int? CourseID { get; set; }

        public int? ClassID { get; set; }

        [Required]
        public int StudentID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; }

        public DateTime? TeacherNotifiedAt { get; set; }

        [ValidateNever]
        public Course? Course { get; set; }

        [ValidateNever]
        public Class? Class { get; set; }

        [ValidateNever]
        public User? Student { get; set; }

        [ValidateNever]
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}