using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int? StyleId { get; set; }
        public int Count { get; set; }
        public DateTime Date { get; set; }
        public int IsDeleted { get; set; }

        public virtual Cart Cart { get; set; }
        public virtual Product Product { get; set; }
        public virtual Style Style { get; set; }
    }
}
