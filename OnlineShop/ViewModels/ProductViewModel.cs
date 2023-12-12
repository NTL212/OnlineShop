using Microsoft.AspNetCore.Http;
using OnlineShop.Models;
using System.Collections.Generic;
using X.PagedList;

namespace OnlineShop.ViewModels
{
    public class ProductViewModel
    {
        public IPagedList<Product> productList { get; set; }
        public string Sort { get; set; }
    }
}
