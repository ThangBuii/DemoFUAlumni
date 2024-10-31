using System;
using System.Collections.Generic;

namespace ApiAngular.Models.Data
{
    public partial class Notification
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public int? PostId { get; set; }
        public string? Message { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual Post? Post { get; set; }
        public virtual User? User { get; set; }
    }
}
