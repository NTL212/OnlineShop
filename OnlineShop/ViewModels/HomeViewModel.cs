using OnlineShop.Models;
using System.Collections.Generic;
using X.PagedList;

namespace OnlineShop.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> productList { get; set; }
        public List<Category> categoryList { get; set; }

    }
}
