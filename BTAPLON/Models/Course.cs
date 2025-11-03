using System.Security.Claims;

namespace BTAPLON.Models
{
    public class Course
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public int? TeacherID { get; set; }

        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? Teacher { get; set; }

        public ICollection<Class>? Classes { get; set; }

    }

}
