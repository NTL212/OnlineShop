using OnlineShop.Models;
using System.Collections.Generic;
using System;
namespace OnlineShop.ViewModels
{
    public class OrderCartViewModel
    {
        public int CartItemId { get; set; }
        public string Image { get; set; }
        public string ProductName { get; set; }
        public decimal PromotionalPrice { get; set; }
        public int Count { get; set; }
        public decimal Total { get; set; }

        public int ProductId { get; set; }
    }
}
