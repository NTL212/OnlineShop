using OnlineShop.Models;
using System.Collections.Generic;

namespace OnlineShop.ViewModels
{
    public class InvoiceViewModel
    {
        public User User { get; set; }
        public Order Order { get; set; }
        public List<OrderCartViewModel> OrderItems { get; set; }
        public decimal Total { get; set; }
    }
}
