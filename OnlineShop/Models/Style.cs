using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class Style
    {
        public Style()
        {
            Products = new HashSet<Product>();
        }

        public int StyleId { get; set; }
        public string StyleName { get; set; }
        public int IsDeleted { get; set; }
        public string Image { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
