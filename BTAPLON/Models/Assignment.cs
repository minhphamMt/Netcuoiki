namespace BTAPLON.Models;
using System.ComponentModel.DataAnnotations;

public class Assignment
{
    public int AssignmentID { get; set; }

    [Required]
    public int ClassID { get; set; }

    [Required]
    public string Title { get; set; }

    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public Class? Class { get; set; }
    public ICollection<Submission>? Submissions { get; set; }
}
