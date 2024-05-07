using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using OnlineShop.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

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
        public async Task<IActionResult> Index(int? page)
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
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            var user = _context.Users.FirstOrDefault(n => n.UserId == userId);
            var orderList = _context.Orders.Include(o => o.Status).Include(o => o.User).Where(o => o.StatusId == 7).OrderByDescending(o => o.Date);
            foreach (var order in orderList)
            {
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Product.SellerId == user.SellerId)
                    {
                        orderList.Where(o => o.OrderId == orderItem.OrderId);
                    }
                }
            }
            return View(orderList.ToPagedList(page ?? 1, 5));
        }
        public async Task<IActionResult> ReceiveOrderList(int? page)
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
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            var onlineShopContext = _context.Orders.Include(o => o.Status).Include(o => o.User).Where(o => (o.StatusId == 2 || o.StatusId == 6) && o.ShipperId==userId).OrderByDescending(o => o.Date);
            return View(onlineShopContext.ToPagedList(page ?? 1, 5));
        }
        public async Task<IActionResult> Details(int? id)
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
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
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

        public async Task<IActionResult> ReceiveDelivery(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            order.StatusId = 2;

            order.ShipperId = int.Parse(HttpContext.Session.GetString("userId"));
            _context.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ReceiveOrderList));
        }

        public async Task<IActionResult> Delivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            order.StatusId = 6;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ReceiveOrderList));
        }
    }
}
