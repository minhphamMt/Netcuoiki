using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BTAPLON.Models
{
    public class NotificationReceipt
    {
        public int NotificationReceiptID { get; set; }

        public int NotificationID { get; set; }

        public int UserID { get; set; }

        public DateTime ReadAt { get; set; }

        [ValidateNever]
        public Notification? Notification { get; set; }

        [ValidateNever]
        public User? User { get; set; }
    }
}