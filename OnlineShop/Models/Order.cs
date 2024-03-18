using System;
using System.Collections.Generic;

#nullable disable

namespace OnlineShop.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderItems = new HashSet<OrderItem>();
        }

        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Receiver { get; set; }
        public int ShipperId { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int StatusId { get; set; }
        public int IsPay { get; set; }
        public string Email { get; set; }
        public DateTime? Date { get; set; }
        public int IsDeleted { get; set; }

        public virtual User Shipper { get; set; }
        public virtual StatusOrder Status { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
