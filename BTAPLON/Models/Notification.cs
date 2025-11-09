using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        [Required]
        public int ClassID { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int CreatedByID { get; set; }

        public DateTime CreatedAt { get; set; }

        [ValidateNever]
        public Class? Class { get; set; }

        [ValidateNever]
        public User? CreatedBy { get; set; }
    }
}