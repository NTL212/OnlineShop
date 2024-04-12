using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShop.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class HomeController : Controller
    {
        private readonly OnlineShopContext _context;
        public HomeController(OnlineShopContext context)
        {
            _context = context;
        }
        public IActionResult Index(string selectedYear)
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
            int year = DateTime.Now.Year;
            if (selectedYear != null)
            {
                year = int.Parse(selectedYear);
            }
            int month = DateTime.Now.Month;
            if (totalRevenue(year) != 0)
            {
                ViewBag.totalRevenue = string.Format("{0:0,0} VNĐ", totalRevenue(year));
                ViewBag.averageRevenue = string.Format("{0:0,0} VNĐ", (totalRevenue(year) / 12));
            }
            else
            {
                ViewBag.totalRevenue = "0 VNĐ";
                ViewBag.averageRevenue = "0 VNĐ";
            }
            if (growth(year) == -1)
            {
                ViewBag.growth = "Chưa có dữ liệu";
            }
            else
            {
                ViewBag.growth = string.Format("{0:0.00}", growth(year)) + "%";
            }
            ViewBag.totalUsers = totalUsers(year);
            ViewBag.totalOrders = totalOrders(year);
            List<decimal> monthlyRevenueLst = new List<decimal>();
            List<int> monthlySold = new List<int>();
            List<int> monthlyOrders = new List<int>();
            for (int i = 1; i <= 12; i++)
            {
                if (monthlyRevenue(i, year) == 0 && i <= month)
                {
                    monthlyRevenueLst.Add(0);
                    monthlySold.Add(0);
                    monthlyOrders.Add(0);
                }
                else if (monthlyRevenue(i, year) == 0 && i > month)
                {
                    monthlyRevenueLst.Add(0);
                    monthlySold.Add(0);
                    monthlyOrders.Add(0);
                }
                else
                {
                    int numSold = _context.OrderItems.Where(n => n.Order.Date.Value.Month == i
                                                              && n.Order.Date.Value.Year == year
                                                              && n.Order.IsPay == 1
                                                              && n.Product.SellerId == userId).Sum(n => n.Count);
                    int numOrders = (from orderItem in _context.OrderItems
                                     join order in _context.Orders on orderItem.OrderId equals order.OrderId
                                     join product in _context.Products on orderItem.ProductId equals product.ProductId
                                     where product.SellerId == userId
                                     select order).Count();
                    monthlyRevenueLst.Add(monthlyRevenue(i, year));
                    monthlySold.Add(numSold);
                    monthlyOrders.Add(numOrders);
                }
            }
            List<int> years = _context.Orders
                            .Join(_context.OrderItems, o => o.OrderId, oi => oi.OrderId, (o, oi) => new { Order = o, OrderItem = oi })
                            .Join(_context.Products, oi => oi.OrderItem.ProductId, p => p.ProductId, (oi, p) => new { Order = oi.Order, Product = p })
                            .Where(op => op.Product.SellerId == userId)
                            .OrderByDescending(n => n.Order.Date.HasValue ? n.Order.Date.Value.Year : 0)
                            .Select(n => n.Order.Date.HasValue ? n.Order.Date.Value.Year : 0)
                            .Distinct()
                            .ToList();
            if (!years.Contains(DateTime.Now.Year))
            {
                years.Add(DateTime.Now.Year);
                years.Sort();
            }
            ViewBag.Years = years;
            ViewBag.monthlyRevenue = monthlyRevenueLst;
            ViewBag.monthlySold = monthlySold;
            ViewBag.monthlyOrders = monthlyOrders;
            ViewBag.selectedYear = year;
            return View();
        }
        public IActionResult MonthDetail(int month, int year)
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
            ViewBag.Month = month;
            ViewBag.Year = year;
            List<Product> products = _context.Products.Where(n => n.IsDeleted == 0
                                                               && n.IsActive == 1
                                                               && n.SellerId == userId)
                                                      .Select(n => new Product
                                                      {
                                                          ProductId = n.ProductId,
                                                          ProductName = n.ProductName,
                                                          PromotionalPrice = n.PromotionalPrice,

                                                      }).Distinct().ToList();
            List<int> solds = new List<int>();
            List<decimal> revenues = new List<decimal>();
            foreach (var product in products)
            {
                var query = _context.OrderItems.Where(n => n.ProductId == product.ProductId
                                                        && n.Order.Date.Value.Month == month
                                                        && n.Order.Date.Value.Year == year
                                                        && n.Order.IsPay == 1
                                                        && n.Order.IsDeleted == 0
                                                        && n.Product.SellerId == userId);
                int sold = query.Sum(n => n.Count);
                decimal revenue = (decimal)query.Sum(n => n.Count * n.Product.PromotionalPrice);
                solds.Add(sold);
                revenues.Add(revenue);
            }
            ViewBag.products = products;
            ViewBag.revenues = revenues;
            ViewBag.solds = solds;
            ViewBag.totalRevenue = revenues.Sum();
            return View();
        }
        public decimal totalRevenue(int year)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            var lst = from s1 in (from s1 in (from s1 in _context.Orders.Where(s1 => s1.IsPay == 1
                                                                               && s1.IsDeleted == 0
                                                                               && s1.Date.Value.Year == year)
                                              join s2 in _context.OrderItems
                                              on s1.OrderId equals s2.OrderId
                                              select new
                                              {
                                                  s1.OrderId,
                                                  s1.Date,
                                                  s2.ProductId,
                                                  s2.Count
                                              })
                                  join s2 in _context.Products.Where(s2 => s2.SellerId == userId)
                                  on s1.ProductId equals s2.ProductId
                                  select new
                                  {
                                      s1.Count,
                                      s2.PromotionalPrice
                                  }
                      )
                      select new
                      {
                          revenue = s1.Count * s1.PromotionalPrice
                      };
            decimal totalRevenue = decimal.Parse(lst.Sum(n => n.revenue).ToString());
            return totalRevenue;
        }
        public decimal monthlyRevenue(int month, int year)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if (_context.Orders.Where(n => n.Date.Value.Year == year && n.Date.Value.Month == month).Count() > 0)
            {
                var lst = from s1 in (from s1 in (from s1 in _context.Orders.Where(s1 => s1.IsPay == 1
                                                                                    && s1.IsDeleted == 0
                                                                                    && s1.Date.Value.Month == month
                                                                                    && s1.Date.Value.Year == year)
                                                  join s2 in _context.OrderItems
                                                  on s1.OrderId equals s2.OrderId
                                                  select new
                                                  {
                                                      s1.OrderId,
                                                      s1.Date,
                                                      s2.ProductId,
                                                      s2.Count
                                                  })
                                      join s2 in _context.Products.Where(s2 => s2.SellerId == userId)
                                      on s1.ProductId equals s2.ProductId
                                      select new
                                      {
                                          s1.Count,
                                          s2.PromotionalPrice
                                      }
                          )
                          select new
                          {
                              revenue = s1.Count * s1.PromotionalPrice
                          };
                decimal monthlyRevenue = decimal.Parse(lst.Sum(n => n.revenue).ToString());
                return monthlyRevenue;
            }
            return 0;
        }
        public int totalUsers(int year)
        {
            int totalUser = _context.Users.Where(n => n.Date.Value.Year == year).Count();
            return totalUser;
        }
        public int totalOrders(int year)
        {
            int userId;
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            int totalOrders = (from orderItem in _context.OrderItems
                           join order in _context.Orders.Where(n => n.Date.Value.Year == year) on orderItem.OrderId equals order.OrderId
                           join product in _context.Products.Where(n => n.SellerId == userId) on orderItem.ProductId equals product.ProductId
                           where product.SellerId == userId
                           select order).Distinct().Count();
            return totalOrders;
        }
        public decimal growth(int year)
        {
            try
            {
                decimal lastYear = totalRevenue(year - 1);
                decimal theYearBeforeLast = totalRevenue(year - 2);
                decimal growth = (lastYear - theYearBeforeLast) / theYearBeforeLast * 100;
                return growth;
            }
            catch
            {
                return -1;
            }
        }

        public IActionResult CSKH()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user =  await _context.Users.FirstOrDefaultAsync(u=>u.UserId.Equals(userId));
           
            if(user == null)
            {
                return BadRequest("Không tìm thấy user");
            }
            else
			{
                var userObj = new
                {
                    userId = user.UserId,
                    userName = user.UserName,
                    avatar = user.Avatar,
                };

				return Ok(userObj);
            }    
        }
    }
}
