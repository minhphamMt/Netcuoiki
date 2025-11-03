using System.ComponentModel.DataAnnotations;

namespace BTAPLON.Models
{
    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        [Required(ErrorMessage = "Class is required")]
        public int? ClassID { get; set; }

        [Required(ErrorMessage = "Student is required")]
        public int? StudentID { get; set; }


        public DateTime EnrolledAt { get; set; }

        public Class? Class { get; set; }
        public User? Student { get; set; }

    }
}
