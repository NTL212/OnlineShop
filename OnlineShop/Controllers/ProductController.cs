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
using OnlineShop.Payment;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace OnlineShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly OnlineShopContext _context;
        private readonly IConfiguration _configuration;
        public ProductController(ILogger<ProductController> logger, OnlineShopContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index(int? page, string? categoryName, string? Sort)
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
            var results = _context.Products.Include(p => p.Category)
            .Where(p => p.ProductName.Contains(keyword)).ToPagedList(page ?? 1, 5);
            //var categoryList = _context.Categories.ToList();
            //ViewData["Categories"] = categoryList;
            var productVM = new ProductViewModel();
            productVM.productList = results;
            ViewBag.Sort = new List<String> { "Mới nhất", "Giá lớn nhất", "Giá thấp nhất" };
            return View("Index", productVM);
        }
        public IActionResult Detail(int id, string mess, bool flagComment = false)
        {
            if(mess != null)
            {
                ViewBag.mess = mess;
            }
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            ViewBag.flagComment = flagComment;
            if (isNum)
            {
                string roleName = HttpContext.Session.GetString("roleName");
                if (roleName != "Customer")
                {
                    return RedirectToAction("Index", "Home", new { area = roleName });
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
            var product = _context.Products.Include(p => p.Category).Include(p => p.Seller).FirstOrDefault(p=>p.ProductId.Equals(id));

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
		public async Task<IActionResult> OrderProduct(string receiver, string email, string phone, string address, int productId, int count, string paymentOption)
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
            if (paymentOption == "4")
            {
                return RedirectToAction("Index", "Product");
            }
            else
            {   
                var url = URLPayment(int.Parse(paymentOption), newOrderId);
                return Redirect(url);
            }
        }
        public string URLPayment(int TypePaymentVN, int orderId)
        {
            var urlPayment = "";
            var order = _context.Orders.FirstOrDefault(x => x.OrderId == orderId);
            var totalAmount = _context.Orders
           .Where(o => o.OrderId == order.OrderId)
           .SelectMany(o => o.OrderItems)
           .Sum(oi => oi.Count * oi.Product.PromotionalPrice);
            //Get Config Info
            string vnp_Returnurl = _configuration["VnpSettings:ReturnUrl"]; //URL nhan ket qua tra ve 
            string vnp_Url = _configuration["VnpSettings:Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = _configuration["VnpSettings:TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = _configuration["VnpSettings:HashSecret"]; //Secret Key
                                                                              //Get payment input
                                                                              //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (totalAmount * 100).ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            if (TypePaymentVN == 2)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (TypePaymentVN == 3)
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            var user = _context.Users.FirstOrDefault(u => u.UserId == order.UserId);
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng của user :" + user.UserName);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            //log.InfoFormat("VNPAY URL: {0}", paymentUrl);
            return urlPayment;
        }

        [HttpGet]
        public async Task<IActionResult> getAllCommnentByProductId(int id)
        {
            var comments = await _context.Comments.Where(c=>c.ProductId == id && c.IsDeleted==0).OrderByDescending(c=>c.Date).ToListAsync();
            if(comments.Any())
            {
                // Chuyển đổi danh sách List<Comment> thành chuỗi JSON
                var jsonComments = JsonConvert.SerializeObject(comments);
                return Ok(comments);
            }
            else
            {
                return BadRequest("không có comments");
            }
        }
        [HttpPost]
		public IActionResult CreateComment(string content, int rating, int productId, int userId)
		{
            var commnent = new Comment();
            commnent.UserId = userId;
            commnent.ProductId = productId;
            commnent.Content = content;
            commnent.Rate = rating;
            commnent.Date = DateTime.Now;
            commnent.IsDeleted = 0;
            _context.Comments.Add(commnent);
            _context.SaveChanges();
			return RedirectToAction("Detail", "Product", new { id =productId, mess="" });
		}
	}
}
