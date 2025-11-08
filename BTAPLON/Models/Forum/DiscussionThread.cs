using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models.Forum
{
    public class DiscussionThread
    {
        public int DiscussionThreadID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? CourseID { get; set; }

        public int? ClassID { get; set; }

        [Required]
        public int CreatedByID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsLocked { get; set; }

        [ValidateNever]
        public Course? Course { get; set; }

        [ValidateNever]
        public Class? Class { get; set; }

        [ValidateNever]
        public User? CreatedBy { get; set; }

        [ValidateNever]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}