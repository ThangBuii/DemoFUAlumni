using System;
using System.Collections.Generic;

namespace ApiAngular.Models.Data
{
    public partial class User
    {
        public User()
        {
            Notifications = new HashSet<Notification>();
            Posts = new HashSet<Post>();
            Tags = new HashSet<Tag>();
        }

        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string StudentNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Role { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
    }
}
