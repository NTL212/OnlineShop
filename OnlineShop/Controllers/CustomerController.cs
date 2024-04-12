using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using MimeKit;
using MailKit.Net.Smtp;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using OnlineShop.ViewModels;
using System.Reflection;
using Rotativa.AspNetCore;

namespace OnlineShop.Controllers
{
    public class CustomerController : Controller
    {
        private readonly OnlineShopContext _context;
        private readonly IHostingEnvironment _environment;

        public CustomerController(OnlineShopContext context, IHostingEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        [HttpGet]
        public IActionResult GetSessionValue(string key)
        {
            var value = HttpContext.Session.GetString(key);
            return Json(value);
        }

        [HttpGet]
        public IActionResult GetSellerInfo(int productId)
        {
            var product = _context.Products.Find(productId);
            var seller = _context.Users.Find(product.SellerId);
            return Json(seller);
        }
        public IActionResult SignIn()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn([Bind("Email", "Password")] User user)
        {
            if (ModelState.IsValid)
            {
                List<User> lst = _context.Users.Where(n => n.Email == user.Email
                                                        && n.Password == encryptPassword(user.Password)).ToList();
                                                        //&& n.Password == user.Password).ToList();
                if (lst.Count() > 0)
                {
                    HttpContext.Session.SetString("userId", lst[0].UserId.ToString());
                    HttpContext.Session.SetString("userName", lst[0].UserName);
                    HttpContext.Session.SetString("avatar", lst[0].Avatar);
                    if (lst[0].IsDeleted == 1)
                    {
                        ViewBag.mess = "Tài khoản hiện đang bị khóa";
                        return View();
                    }
                    if (lst[0].RoleId == 1)
                    {
                        HttpContext.Session.SetString("roleName", "Admin");
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else if (lst[0].RoleId == 2) {
                        HttpContext.Session.SetString("roleName", "Customer");
                        return RedirectToAction("Index", "Home");
                    }
                    else if (lst[0].RoleId == 3)
                    {
                        HttpContext.Session.SetString("roleName", "Seller");
                        return RedirectToAction("Index", "Home", new { area = "Seller" });
                    }
                    else if (lst[0].RoleId == 4)
                    {
                        HttpContext.Session.SetString("roleName", "Shipper");
                        return RedirectToAction("Index", "Home", new { area = "Shipper" });
                    }
                }
                ViewBag.mess = "Email hoặc mật khẩu không chính xác";
            }
            return View();
        }
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp([Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,RoleId,Address,Avatar,Date,IsDeleted")] User user)
        {
            if (ModelState.IsValid)
            {
                if(user.UserName == null || user.IdCard == null || user.Email == null || 
                    user.Phone == null || user.Password == null || user.Address == null)
                {
                    ViewBag.mess = "Vui lòng điền đẩy đủ thông tin";
                    return View();
                }
                HttpContext.Session.SetString("username", user.UserName);
                HttpContext.Session.SetString("idCard", user.IdCard);
                HttpContext.Session.SetString("email", user.Email);
                HttpContext.Session.SetString("phone", user.Phone);
                HttpContext.Session.SetString("password", encryptPassword(user.Password));
                //HttpContext.Session.SetString("password", user.Password);
                HttpContext.Session.SetString("address", user.Address);
                HttpContext.Session.SetString("avatar", "default.jpg");
                var lst = _context.Users.ToList();
                foreach(var item in lst)
                {
                    if(item.IdCard == user.IdCard)
                    {
                        ViewBag.mess = "ID đã tồn tại";
                        return View();
                    }
                    if(item.Email == user.Email)
                    {
                        ViewBag.mess = "Email đã tồn tại";
                        return View();
                    }
                    if(user.Phone == item.Phone)
                    {
                        ViewBag.mess = "SĐT đã tồn tại";
                        return View();
                    }
                }
                Random ran = new Random();
                int otp = ran.Next(100000, 999999);
                HttpContext.Session.SetInt32("otp", otp);
                var mess = new MimeMessage();
                mess.From.Add(new MailboxAddress("OnlineShop", "ute.onlineshop.project@gmail.com"));
                mess.To.Add(new MailboxAddress("Xác Thực", user.Email));
                mess.Subject = "Xác Thực Email";
                mess.Body = new TextPart("plain")
                {
                    Text = "OTP: " + otp

                };
                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("ute.onlineshop.project@gmail.com", "yefqpzkttpnqeemo");
                    client.Send(mess);
                    client.Disconnect(true);
                }
                return RedirectToAction(nameof(Auth));
            }
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword([Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,RoleId,Address,Avatar,Date,IsDeleted")] User user)
        {
            if (ModelState.IsValid)
            {
                var lst = _context.Users.Where(n => n.Email == user.Email).ToList();
                if (lst.Count() == 0)
                {
                    ViewBag.mess = "Không tìm thấy email";
                    return View();
                }
                HttpContext.Session.SetString("email", user.Email);
                HttpContext.Session.SetString("password", encryptPassword(user.Password));
                //HttpContext.Session.SetString("password", user.Password);
                Random ran = new Random();
                int otp = ran.Next(100000, 999999);
                HttpContext.Session.SetInt32("otp", otp);
                var mess = new MimeMessage();
                mess.From.Add(new MailboxAddress("OnlineShop", "ute.onlineshop.project@gmail.com"));
                mess.To.Add(new MailboxAddress("Xác Thực", user.Email));
                mess.Subject = "Xác Thực Email";
                mess.Body = new TextPart("plain")
                {
                    Text = "OTP: " + otp

                };
                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("ute.onlineshop.project@gmail.com", "yefqpzkttpnqeemo");
                    client.Send(mess);
                    client.Disconnect(true);
                }
                return RedirectToAction(nameof(Auth));
            }
            return View();
        }

        public IActionResult Auth()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Auth([Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,RoleId,Address,Avatar,Date,IsDeleted")] User user)
        {
            int otp = (int) HttpContext.Session.GetInt32("otp");
            if (ModelState.IsValid)
            {
                if (user.UserId == otp)
                {
                    if (HttpContext.Session.GetString("username") != null)
                    {     
                        //Create new account for customer
                        User customer = new User();
                        customer.UserName = HttpContext.Session.GetString("username");
                        customer.IdCard = HttpContext.Session.GetString("idCard");
                        customer.Email = HttpContext.Session.GetString("email");
                        customer.Phone = HttpContext.Session.GetString("phone");
                        customer.IsEmailActive = 1;
                        customer.Password = HttpContext.Session.GetString("password");
                        customer.RoleId = 2;
                        customer.Address = HttpContext.Session.GetString("address");
                        customer.Avatar = HttpContext.Session.GetString("avatar");
                        customer.Date = DateTime.Now;
                        customer.IsDeleted = 0;
                        _context.Users.Add(customer);
                        await _context.SaveChangesAsync();

                        HttpContext.Session.Clear();
                        HttpContext.Session.Remove("username");
                        HttpContext.Session.Remove("idCard");
                        HttpContext.Session.Remove("email");
                        HttpContext.Session.Remove("phone");
                        HttpContext.Session.Remove("password");
                        HttpContext.Session.Remove("address");
                        HttpContext.Session.Remove("avatar");

                        //Create new cart for new account
                        var lst = _context.Users.Where(x => x.Email == customer.Email).ToList();
                        Cart cart = new Cart();
                        cart.UserId = lst[0].UserId;
                        _context.Add(cart);
                        await _context.SaveChangesAsync();
                    
                        //Redirect to homepage
                        customer = _context.Users.FirstOrDefault(x => x.Email == customer.Email);
                        HttpContext.Session.SetString("userId", customer.UserId.ToString());
                        HttpContext.Session.SetString("roleName", "Customer");
                        HttpContext.Session.SetString("avatar", customer.Avatar);
                        return RedirectToAction("Index", "Product");
                    }
                    else if (HttpContext.Session.GetString("username") == null)
                    {
                        //Reset password for customer
                        User customer = _context.Users.FirstOrDefault(n => n.Email == HttpContext.Session.GetString("email"));
                        customer.Password = HttpContext.Session.GetString("password");
                        _context.Users.Update(customer);
                        await _context.SaveChangesAsync();

                        HttpContext.Session.Clear();
                        HttpContext.Session.Remove("email");
                        HttpContext.Session.Remove("password");
                        
                        customer = _context.Users.FirstOrDefault(x => x.Email == customer.Email);
                        HttpContext.Session.SetString("userId", customer.UserId.ToString());
                        HttpContext.Session.SetString("roleName", "Customer");
                        HttpContext.Session.SetString("avatar", customer.Avatar);
                        return RedirectToAction("Index", "Product");
                    }
                }
                else
                {
                    ViewBag.mess = "OTP không chính xác";
                    return View();
                }
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
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
            User user = await _context.Users
               .Include(u => u.Role)
               .FirstOrDefaultAsync(m => m.UserId == userId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(IFormFile Avatar, [Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,RoleId,Address,Avatar,Date,IsDeleted")] User user)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (ModelState.IsValid)
            {
                try
                {
                    if (Avatar != null)
                    {
                        user.Avatar = Avatar.FileName;
                        var uploadDirectory = Path.Combine(_environment.WebRootPath, "upload", "images", "avatars", "customer");
                        if (!Directory.Exists(uploadDirectory))
                        {
                            Directory.CreateDirectory(uploadDirectory);
                        }
                        var path = Path.Combine(uploadDirectory, Avatar.FileName);
                        using var fileStream = new FileStream(path, FileMode.Create);
                        await Avatar.CopyToAsync(fileStream);
                        if (userId.ToString() == HttpContext.Session.GetString("userId"))
                        {
                            HttpContext.Session.SetString("avatar", Avatar.FileName);
                        }
                    }
                    else
                    {
                        user.Avatar = _context.Users.AsNoTracking().FirstOrDefault(n => n.UserId == userId).Avatar;
                    }
                    foreach (PropertyInfo pi in user.GetType().GetProperties())
                    {
                        if (pi.PropertyType == typeof(string))
                        {
                            string value = (string)pi.GetValue(user);
                            if (string.IsNullOrEmpty(value))
                            {
                                string roleName = HttpContext.Session.GetString("roleName");
                                if (!isNum)
                                {
                                    return RedirectToAction("SignIn", "Customer", new { area = "Default" });
                                }
                                if (roleName != "Customer")
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
                                user = _context.Users.AsNoTracking().FirstOrDefault(m => m.UserId == userId);
                                ViewBag.mess = "Vui lòng điển đẩy đủ thông tin";
                                return View(user);
                            }
                        }
                    }
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Profile));
            }
            return View(user);
        }

        public async Task<IActionResult> Orders()
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
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
            List<Order> orders = _context.Orders.Include(o=>o.Status).Where(o=>o.UserId== userId).OrderByDescending(o => o.Date).ToList();
            ViewBag.numOfOrders = orders.Count;
            return View(orders);
        }
        public async Task<IActionResult> OrderDetail(int id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Customer")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            try
            {
                Order order = _context.Orders.Include(o => o.Status).Where(o => o.OrderId == id).FirstOrDefault();
                if (order.UserId != userId || order == null)
                {
                    return RedirectToAction("Orders", "Customer");
                }
                var query = from s1 in _context.OrderItems.Where(s1 => s1.OrderId == id)
                            select new OrderCartViewModel
                            {
                                ProductName = s1.Product.ProductName,
                                Count = s1.Count,
                                Total = (decimal)s1.Product.PromotionalPrice * s1.Count
                            };
                List<OrderCartViewModel> lst = query.ToList();
                ViewBag.total = lst.Sum(n => n.Total);
                ViewBag.lst = lst;
                return View(order);
            }
            catch
            {
                return RedirectToAction("Orders", "Customer");
            }
        }
        public async Task<IActionResult> CancelOrder(int id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Customer")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            try
            {
                Order order = _context.Orders.Include(o => o.Status).Where(o => o.OrderId == id).FirstOrDefault();
                if (order.UserId != userId || order == null)
                {
                    return RedirectToAction("Orders", "Customer");
                }
                var query = from s1 in _context.OrderItems.Where(s1 => s1.OrderId == id)
                            select new OrderCartViewModel
                            {
                                ProductName = s1.Product.ProductName,
                                Count = s1.Count,
                                Total = (decimal)s1.Product.PromotionalPrice * s1.Count
                            };
                List<OrderCartViewModel> lst = query.ToList();
                ViewBag.total = lst.Sum(n => n.Total);
                ViewBag.lst = lst;
                return View(order);
            }
            catch
            {
                return RedirectToAction("Orders", "Customer");
            }
        }

        [HttpPost, ActionName("CancelOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            order.StatusId = 4;
            _context.Update(order);
            await _context.SaveChangesAsync();
            List<OrderItem> orderItems = _context.OrderItems.Where(n => n.OrderId == id).ToList();
            foreach (OrderItem orderItem in orderItems)
            {
                Product product = _context.Products.FirstOrDefault(n => n.ProductId == orderItem.ProductId);
                product.Quantity += orderItem.Count;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        public async Task<IActionResult> Invoice(int id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Customer")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            try
            {
                User user = _context.Users.Find(userId);
                Order order = _context.Orders.Include(o => o.Status).Where(o => o.OrderId == id).FirstOrDefault();
                if (order.UserId != userId || order == null)
                {
                    return RedirectToAction("Orders", "Customer");
                }
                var query = from s1 in _context.OrderItems.Where(s1 => s1.OrderId == id)
                            select new OrderCartViewModel
                            {
                                ProductName = s1.Product.ProductName,
                                PromotionalPrice = (decimal)s1.Product.PromotionalPrice,
                                Count = s1.Count,
                                Total = (decimal)s1.Product.PromotionalPrice * s1.Count
                            };
                List<OrderCartViewModel> orderItems = query.ToList();
                InvoiceViewModel invoice = new InvoiceViewModel
                {
                    User = user,
                    Order = order,
                    OrderItems = orderItems,
                    Total = orderItems.Sum(n => n.Total)
                };
                return new ViewAsPdf(invoice)
                {
                    FileName = $"Invoice_{invoice.Order.OrderId}.pdf"
                };
            }
            catch
            {
                return RedirectToAction("Orders", "Customer");
            }
        }

        public string encryptPassword(string password)
        {
            if(password == null)
            {
                return "";
            }
            string key = "@a1235&%%@Dacxs";
            password += key;
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordBytes);
        }
        public IActionResult SignOut()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("userId");
            HttpContext.Session.Remove("roleName");
            return RedirectToAction("Index", "Home");
        }
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
