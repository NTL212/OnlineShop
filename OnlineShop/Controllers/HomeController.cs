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
                ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            }
            var productList = _context.Products.Include(p => p.Category).Include(p => p.Style).ToPagedList(page ?? 1, 5);
            var categoryList = _context.Categories.ToList();
            var homeViewModel = new HomeViewModel();
            homeViewModel.productList = productList;
            homeViewModel.categoryList = categoryList;
            //ViewData["Categories"] = categoryList;
            return View(homeViewModel);
		}
        public IActionResult Privacy(string test)
        {
            ViewBag.Test = test;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
