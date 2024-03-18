using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class Product
    {
        public Product()
        {
            CartItems = new HashSet<CartItem>();
            OrderItems = new HashSet<OrderItem>();
        }

        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public string ProductName { get; set; }
        public string Decription { get; set; }
        public double Price { get; set; }
        public double PromotionalPrice { get; set; }
        public int Quantity { get; set; }
        public int Sold { get; set; }
        public int IsActive { get; set; }
        public string Image { get; set; }
        public int CategoryId { get; set; }
        public int StyleId { get; set; }
        public double Rating { get; set; }
        public DateTime? Date { get; set; }
        public int IsDeleted { get; set; }

        public virtual Category Category { get; set; }
        public virtual User Seller { get; set; }
        public virtual Style Style { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
