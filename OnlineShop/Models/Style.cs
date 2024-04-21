using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class Style
    {
        public int StyleId { get; set; }
        public int? ProductId { get; set; }
        public string StyleName { get; set; }
        public int IsDeleted { get; set; }
    }
}
