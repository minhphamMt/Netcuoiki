using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace BTAPLON.Models.ViewModels
{
    public class EnrollmentBulkCreateViewModel
    {
        [Required(ErrorMessage = "Class is required")]
        public int? ClassID { get; set; }

        public List<int> StudentIDs { get; set; } = new();

        public IEnumerable<SelectListItem> ClassOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> StudentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}