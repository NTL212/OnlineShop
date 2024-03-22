using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models;
using System.Linq;

namespace OnlineShop.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    public class HomeController : Controller
    {
        private readonly OnlineShopContext _context;
        public HomeController(OnlineShopContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (!isNum)
            {
                return RedirectToAction("SignIn", "Customer", new { area = "Default" });
            }
            if (roleName != "Shipper")
            {
                return RedirectToAction("Index", "Product", new { area = "Default" });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            return View();
        }
    }
}
