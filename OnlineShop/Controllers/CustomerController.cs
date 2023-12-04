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
                    HttpContext.Session.SetString("avatar", lst[0].Avatar);
                    if (lst[0].RoleId == 1)
                    {
                        HttpContext.Session.SetString("roleName", "Admin");
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else if (lst[0].RoleId == 2) {
                        HttpContext.Session.SetString("roleName", "Customer");
                        return RedirectToAction("Index", "Product");
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
                HttpContext.Session.SetString("username", user.UserName);
                HttpContext.Session.SetString("idCard", user.IdCard);
                HttpContext.Session.SetString("email", user.Email);
                HttpContext.Session.SetString("phone", user.Phone);
                HttpContext.Session.SetString("password", encryptPassword(user.Password));
                //HttpContext.Session.SetString("password", user.Password);
                HttpContext.Session.SetString("address", user.Address);
                HttpContext.Session.SetString("avatar", user.Avatar);
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
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
            ViewBag.quantity = _context.CartItems.Where(n => n.CartId == cartId).Count();
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
            var av = Avatar;
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
            return View();
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
            return RedirectToAction("Index", "Product");
        }
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
