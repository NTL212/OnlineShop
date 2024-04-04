using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace OnlineShop.Areas.Seller.Controllers
{
	[Area("Seller")]
	public class ShipperController : Controller
    {
		
        private readonly OnlineShopContext _context;
        private readonly IHostingEnvironment _environment;
        public ShipperController(OnlineShopContext context, IHostingEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		public IActionResult Index(int? page)
        {

			int userId;
			string roleName = HttpContext.Session.GetString("roleName");
			bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
			if (!isNum)
			{
				return RedirectToAction("SignIn", "Customer", new { area = "Default" });
			}
			if (roleName != "Seller")
			{
				return RedirectToAction("Index", "Home", new { area = roleName });
			}
			ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
			var onlineShopContext = _context.Users.Include(u => u.Role).Where(n=>n.SellerId==userId);
			return View(onlineShopContext.ToPagedList(page ?? 1, 5));
		}

        // GET: Seller/Shipper/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Seller")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/AdminUsers/Create
        public IActionResult Create()
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Seller")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View();
        }

        // POST: Admin/AdminUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile Avatar, [Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,Address,Avatar,IsDeleted")] User user)
        {
            if (ModelState.IsValid)
            {
                user.Password = encryptPassword(user.Password);
                user.RoleId = 4;
                user.SellerId = int.Parse(HttpContext.Session.GetString("userId"));
                user.Date = DateTime.Now;
                if (Avatar != null)
                {
                    user.Avatar = Avatar.FileName;
                    var uploadDirectory = Path.Combine(_environment.WebRootPath, "upload", "images", "avatars", "customer");
                    if (user.RoleId == 1)
                    {
                        uploadDirectory = Path.Combine(_environment.WebRootPath, "upload", "images", "avatars", "admin");
                    }
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }
                    var path = Path.Combine(uploadDirectory, Avatar.FileName);
                    using var fileStream = new FileStream(path, FileMode.Create);
                    await Avatar.CopyToAsync(fileStream);
                }
                foreach (PropertyInfo pi in user.GetType().GetProperties())
                {
                    if (pi.PropertyType == typeof(string))
                    {
                        string value = (string)pi.GetValue(user);
                        if (string.IsNullOrEmpty(value))
                        {
                            ViewBag.mess = "Vui lòng điển đẩy đủ thông tin";
                            return View();
                        }
                    }
                }
                var lst = _context.Users.ToList();
                foreach (var item in lst)
                {
                    if (item.IdCard == user.IdCard)
                    {
                        ViewBag.mess = "ID đã tồn tại";
                        return View();
                    }
                    if (item.Email == user.Email)
                    {
                        ViewBag.mess = "Email đã tồn tại";
                        return View();
                    }
                    if (user.Phone == item.Phone)
                    {
                        ViewBag.mess = "SĐT đã tồn tại";
                        return View();
                    }
                }
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                User customer = _context.Users.FirstOrDefault(x => x.Email == user.Email);
                if (customer.RoleId == 2)
                {
                    Cart cart = new Cart();
                    cart.UserId = customer.UserId;
                    _context.Add(cart);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/AdminUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Seller")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Admin/AdminUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Password, IFormFile Avatar,string Email, [Bind("UserId,UserName,IdCard,Email,Phone,IsEmailActive,Password,RoleId,Address,Avatar,Date,IsDeleted")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                   user.Password = _context.Users.AsNoTracking().FirstOrDefault(u=>u.UserId == id).Password;
                    user.RoleId = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == id).RoleId;
                    user.SellerId = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == id).SellerId;
                    user.Date = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == id).Date;
                    if (Password != null)
                    {
                        user.Password = encryptPassword(Password);
                    }
                    if (Avatar != null)
                    {
                        user.Avatar = Avatar.FileName;
                        var uploadDirectory = Path.Combine(_environment.WebRootPath, "upload", "images", "avatars", "customer");
                        if (user.RoleId == 1)
                        {
                            uploadDirectory = Path.Combine(_environment.WebRootPath, "upload", "images", "avatars", "admin");
                        }
                        if (!Directory.Exists(uploadDirectory))
                        {
                            Directory.CreateDirectory(uploadDirectory);
                        }
                        var path = Path.Combine(uploadDirectory, Avatar.FileName);
                        using var fileStream = new FileStream(path, FileMode.Create);
                        await Avatar.CopyToAsync(fileStream);
                        if (id.ToString() == HttpContext.Session.GetString("userId"))
                        {
                            HttpContext.Session.SetString("avatar", Avatar.FileName);
                        }
                    }
                    var existingUserWithSameIdCard = _context.Users.FirstOrDefault(u => u.IdCard == user.IdCard && u.UserId != user.UserId);
                    if (existingUserWithSameIdCard != null)
                    {
                        ViewBag.mess = "ID đã tồn tại";
                        return View();
                    }

                    var existingUserWithSameEmail = _context.Users.FirstOrDefault(u => u.Email == user.Email && u.UserId != user.UserId);
                    if (existingUserWithSameEmail != null)
                    {
                        ViewBag.mess = "Email đã tồn tại";
                        return View();
                    }

                    var existingUserWithSamePhone = _context.Users.FirstOrDefault(u => u.Phone == user.Phone && u.UserId != user.UserId);
                    if (existingUserWithSamePhone != null)
                    {
                        ViewBag.mess = "SĐT đã tồn tại";
                        return View();
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Admin/AdminUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Seller")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user.IsDeleted == 0)
            {
                user.IsDeleted = 1;
            }
            else if (user.IsDeleted == 1)
            {
                user.IsDeleted = 0;
            }
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
        public string encryptPassword(string password)
        {
            if (password == null)
            {
                return "";
            }
            string key = "@a1235&%%@Dacxs";
            password += key;
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordBytes);
        }
    }
}
