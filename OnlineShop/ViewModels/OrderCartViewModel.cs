using OnlineShop.Models;
using System.Collections.Generic;
using System;
namespace OnlineShop.ViewModels
{
    public class OrderCartViewModel
    {
        public string ProductName { get; set; }
        public int Count { get; set; }
        public decimal Total { get; set; }
    }
}
