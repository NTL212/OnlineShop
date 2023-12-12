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

        public IActionResult Index(int? page, string? categoryName, string? Sort)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (isNum)
            {
                if(_context.Users.FirstOrDefault(n => n.UserId == userId).RoleId == 1)
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
                int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
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
            var productList = _context.Products.AsQueryable();
            if (categoryName != null)
            {
                productList = productList.Where(p => p.Category.CategoryName.Contains(categoryName));

            }
            
            if (Sort != null && Sort.Contains("Mới nhất"))
            {
                productList = productList.OrderByDescending(p => p.Date);
            }
            else if (Sort != null && Sort.Contains("Giá lớn nhất"))
            {
                productList = productList.OrderByDescending(p => p.Price);
            }
            else if (Sort != null && Sort.Contains("Giá thấp nhất"))
            {
                productList = productList.OrderBy(p => p.Price);
            }
            //var categoryList = _context.Categories.ToList();
            //ViewData["Categories"] = categoryList;
            var productVM = new ProductViewModel();
            productVM.productList = productList.ToPagedList(page ?? 1, 6);
            ViewBag.Sort = new List<String> { "Mới nhất", "Giá lớn nhất", "Giá thấp nhất" };
            return View(productVM);
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
        public IActionResult Detail(int id, string mess)
        {
            if(mess != null)
            {
                ViewBag.mess = mess;
            }
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (isNum)
            {
                ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
                int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
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
		public IActionResult OrderProduct(int productId, int quantity)
		{
            if (quantity <= 0)
            {
                quantity = 1;
            }
            if (quantity > _context.Products.FirstOrDefault(n => n.ProductId == productId).Quantity)
            {
                string mess = "Mua thất bại";
                return RedirectToAction("Detail", "Product", new { id = productId, mess = mess });
            }
            int userId;
			string roleName = HttpContext.Session.GetString("roleName");
			bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
			if (!isNum)
			{
				return RedirectToAction("SignIn", "Customer", new { area = "Default" });
			}
			if (roleName != "Customer")
			{
				return RedirectToAction("Index", "Home", new { area = "Admin" });
			}
            User user = _context.Users.Where(n => n.UserId == userId).FirstOrDefault();
            Product product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            ViewBag.username = user.UserName;
            int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
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
            List<OrderCartViewModel> cartItems = query.ToList();
            ViewBag.quantity = cartItems.Count;
            ViewBag.cartItems = cartItems;
            ViewBag.totalCartItems = cartItems.Sum(n => n.Total);
            ViewBag.quantity = _context.CartItems.Where(n => n.CartId == cartId).Count();
            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.ProductName;
            ViewBag.Count = quantity;
            ViewBag.Total = product.PromotionalPrice * quantity;
            return View(user);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> OrderProduct(string receiver, string email, string phone, string address, int productId, int count)
		{
            int userId = int.Parse(HttpContext.Session.GetString("userId"));
            if (receiver == null || email == null | phone == null || address == null)
            {
                ViewBag.mess = "Vui lòng điền đầy đủ thông tin trước khi đặt hàng";
                User user = _context.Users.Where(n => n.UserId == userId).FirstOrDefault();
                Product product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
                ViewBag.username = user.UserName;
                int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
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
                List<OrderCartViewModel> cartItems = query.ToList();
                ViewBag.quantity = cartItems.Count;
                ViewBag.cartItems = cartItems;
                ViewBag.totalCartItems = cartItems.Sum(n => n.Total);
                ViewBag.quantity = _context.CartItems.Where(n => n.CartId == cartId).Count();
                ViewBag.ProductId = productId;
                ViewBag.ProductName = product.ProductName;
                ViewBag.Count = count;
                ViewBag.Total = product.PromotionalPrice * count;
                return View(user);
            }
            Order order = new Order
			{
				UserId = userId,
				Receiver = receiver,
				Email = email,
				Phone = phone,
				Address = address,
				StatusId = 1,
				IsPay = 0,
				Date = DateTime.Now,
				IsDeleted = 0
			};
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            int newOrderId = order.OrderId;
            OrderItem orderItem = new OrderItem
            {
                OrderId = newOrderId,
                ProductId = productId,
                Count = count
            };
            Product product1 = _context.Products.FirstOrDefault(n => n.ProductId == productId);
            product1.Quantity -= count;
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Product");
        }
	}
}
