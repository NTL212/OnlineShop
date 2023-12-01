using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShop.Models;
using OnlineShop.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace OnlineShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly OnlineShopContext _context;
        public ProductController(ILogger<ProductController> logger, OnlineShopContext context)
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
            //var categoryList = _context.Categories.ToList();
            //ViewData["Categories"] = categoryList;
            return View(productList);
        }
        [HttpPost]
        public IActionResult Search(int? page, string keyword)
        {
            var results = _context.Products.Include(p => p.Category).Include(p => p.Style)
            .Where(p => p.ProductName.Contains(keyword)).ToPagedList(page ?? 1, 5);
            //var categoryList = _context.Categories.ToList();
            //ViewData["Categories"] = categoryList;
            return View("Index", results);
        }
        public IActionResult Detail(int id)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (isNum)
            {
                ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            }
            var product = _context.Products.Include(p => p.Category).Include(p => p.Style).FirstOrDefault(p=>p.ProductId.Equals(id));

            if (product == null)
            {
                return NotFound(); // Trả về 404 nếu không tìm thấy sản phẩm
            }

            return View(product);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
