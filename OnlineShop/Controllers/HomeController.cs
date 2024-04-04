using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShop.Models;
using OnlineShop.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using X.PagedList;

namespace OnlineShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly OnlineShopContext _context;
		public HomeController(ILogger<HomeController> logger, OnlineShopContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(int? page)
        {   
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (isNum)
            {
                string roleName = HttpContext.Session.GetString("roleName");
                if (roleName != "Customer")
                {
                    return RedirectToAction("Index", "Home", new { area = roleName });
                }
                ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
                var query = from s1 in _context.Carts.Where(s1 => s1.UserId == userId)
                            join s2 in _context.CartItems on s1.CartId equals s2.CartId
                            select new OrderCartViewModel
                            {
                                CartItemId = s2.CartItemId,
                                Image = s2.Product.Image,
                                PromotionalPrice = (decimal)s2.Product.PromotionalPrice,
                                ProductName = s2.Product.ProductName,
                                Count = s2.Count,
                                Total = (decimal)s2.Product.PromotionalPrice * s2.Count
                            };
                List<OrderCartViewModel> lst = query.ToList();
                ViewBag.quantity = lst.Count;
                ViewBag.cartItems = lst;
                ViewBag.totalCartItems = lst.Sum(n => n.Total);
            }
        
            var productList = _context.Products.Include(p => p.Category).Include(p => p.Style).OrderBy(p=>p.Date).Take(8).ToList();
            var categoryList = _context.Categories.ToList();
            var categories = HttpContext.Session.Get("categories");
            if (categories == null)
            {   var new_categories = categoryList.AsQueryable().Select(x=>x.CategoryName).ToList();
                string result = string.Join(",", new_categories);
                HttpContext.Session.SetString("categories", result);
            }
            var homeViewModel = new HomeViewModel();
            homeViewModel.productList = productList;
            homeViewModel.categoryList = categoryList;
            //ViewData["Categories"] = categoryList;
            return View(homeViewModel);
		}
        public IActionResult Privacy(int productId, int count)
        {
            ViewBag.Test = "productId: " + productId + "\nCount: " + count;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
