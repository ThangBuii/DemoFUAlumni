using System;
using System.Collections.Generic;

namespace ApiAngular.Models.Data
{
    public partial class Tag
    {
        public int TagId { get; set; }
        public int? UserId { get; set; }
        public int? PostId { get; set; }
        public DateTime? TaggedAt { get; set; }

        public virtual Post? Post { get; set; }
        public virtual User? User { get; set; }
    }
}
