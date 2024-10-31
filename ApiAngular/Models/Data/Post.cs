using System;
using System.Collections.Generic;

namespace ApiAngular.Models.Data
{
    public partial class Post
    {
        public Post()
        {
            Notifications = new HashSet<Notification>();
            Tags = new HashSet<Tag>();
        }

        public int PostId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; } = null!;
        public string? ImageName { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
    }
}
