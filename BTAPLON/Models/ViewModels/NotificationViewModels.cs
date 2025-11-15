using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTAPLON.Models.ViewModels
{
    public class NotificationIndexViewModel
    {
        public bool CanCreate { get; set; }

        public IList<SelectListItem> TeacherClasses { get; set; } = new List<SelectListItem>();

        public IList<NotificationDisplayViewModel> Notifications { get; set; } = new List<NotificationDisplayViewModel>();

        public NotificationFormInput Form { get; set; } = new NotificationFormInput();

        public string? SearchTerm { get; set; }
    }

    public class NotificationDisplayViewModel
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? ClassCode { get; set; }

        public int ClassId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool CanManage { get; set; }
    }

    public class NotificationFormInput
    {
        [Required(ErrorMessage = "Vui lòng chọn lớp học.")]
        [Display(Name = "Lớp học")]
        public int? ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
        [StringLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung thông báo.")]
        [StringLength(2000, ErrorMessage = "Nội dung thông báo không được vượt quá 2000 ký tự.")]
        [Display(Name = "Nội dung")]
        public string? Content { get; set; }
    }

    public class NotificationEditViewModel
    {
        public int Id { get; set; }

        public string Heading { get; set; } = string.Empty;

        public NotificationFormInput Form { get; set; } = new NotificationFormInput();

        public IList<SelectListItem> TeacherClasses { get; set; } = new List<SelectListItem>();
    }
}