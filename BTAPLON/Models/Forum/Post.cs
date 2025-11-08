using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models.Forum
{
    public class Post
    {
        public int PostID { get; set; }

        [Required]
        public int DiscussionThreadID { get; set; }

        [Required]
        public int AuthorID { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPinned { get; set; }

        [ValidateNever]
        public DiscussionThread? DiscussionThread { get; set; }

        [ValidateNever]
        public User? Author { get; set; }
    }
}