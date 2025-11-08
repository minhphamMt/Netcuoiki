using System.ComponentModel.DataAnnotations;
using BTAPLON.Models.Forum;

namespace BTAPLON.Models.ViewModels
{
    public class ForumIndexViewModel
    {
        public Course? Course { get; set; }
        public Class? Class { get; set; }
        public IList<DiscussionThread> Threads { get; set; } = new List<DiscussionThread>();
        public IList<ForumQuestion> Questions { get; set; } = new List<ForumQuestion>();
    }

    public class ThreadCreateViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public int? CourseID { get; set; }
        public int? ClassID { get; set; }
    }

    public class PostCreateViewModel
    {
        [Required]
        public int DiscussionThreadID { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class QuestionCreateViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public int? CourseID { get; set; }
        public int? ClassID { get; set; }
    }

    public class AnswerCreateViewModel
    {
        [Required]
        public int QuestionID { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}