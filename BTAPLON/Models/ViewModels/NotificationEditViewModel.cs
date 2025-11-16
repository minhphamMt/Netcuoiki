using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTAPLON.Models.ViewModels
{
    public class NotificationEditViewModel
    {
        public int Id { get; set; }

        public string Heading { get; set; } = string.Empty;

        public NotificationFormInput Form { get; set; } = new NotificationFormInput();

        public IList<SelectListItem> TeacherClasses { get; set; } = new List<SelectListItem>();
    }
}