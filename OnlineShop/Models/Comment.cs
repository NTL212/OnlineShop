using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class Comment
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string Content { get; set; }
        public int Rate { get; set; }
        public DateTime? Date { get; set; }
        public int IsDeleted { get; set; }
    }
}
