using System;

namespace BTAPLON.Models.ViewModels
{
    public class StudentAssignmentListItemViewModel
    {
        public int AssignmentID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ClassCode { get; set; }
        public string? CourseName { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class StudentAssignmentDetailViewModel
    {
        public Assignment Assignment { get; set; } = null!;
        public Submission? Submission { get; set; }

        public bool IsOverdue => Assignment.DueDate.HasValue && Assignment.DueDate.Value < DateTime.Now;
    }
}