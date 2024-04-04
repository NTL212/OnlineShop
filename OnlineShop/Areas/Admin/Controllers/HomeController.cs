using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using OnlineShop.ViewModels;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            if (roleName.Equals("Admin") == false)
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            int year = DateTime.Now.Year;
            if(selectedYear != null)
            {
                year = int.Parse(selectedYear);
            }
            int month = DateTime.Now.Month;
            if(totalRevenue(year) != 0)
            {
                ViewBag.totalRevenue = string.Format("{0:0,0} VNĐ", totalRevenue(year));
                ViewBag.averageRevenue = string.Format("{0:0,0} VNĐ", (totalRevenue(year) / 12));
            }
            else
            {
                ViewBag.totalRevenue = "0 VNĐ";
                ViewBag.averageRevenue = "0 VNĐ";
            }
            if(growth(year) == -1)
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
                                                              && n.Order.IsPay == 1).Sum(n => n.Count);
                    int numOrders = _context.Orders.Where(n => n.Date.Value.Month == i
                                                              && n.Date.Value.Year == year
                                                              && n.IsPay == 1).Count();
                    monthlyRevenueLst.Add(monthlyRevenue(i, year));
                    monthlySold.Add(numSold);
                    monthlyOrders.Add(numOrders);
                }
            }
            List<int> years = _context.Orders
                            .OrderByDescending(n => n.Date.HasValue ? n.Date.Value.Year : 0)
                            .Select(n => n.Date.HasValue ? n.Date.Value.Year : 0)
                            .Distinct()
                            .ToList();
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
            if (roleName != "Admin")
            {
                return RedirectToAction("Index", "Home", new { area = roleName });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            ViewBag.Month = month;
            ViewBag.Year = year;
            List<Product> products = _context.Products.Where(n => n.IsDeleted == 0
                                                               && n.IsActive == 1)
                                                      .Select(n => new Product
                                                      {
                                                          ProductId = n.ProductId,
                                                          ProductName = n.ProductName,
                                                          PromotionalPrice = n.PromotionalPrice,

                                                      }).Distinct().ToList();
            List<int> solds = new List<int>();
            List<decimal> revenues = new List<decimal>();
            foreach(var product in products)
            {
                var query = _context.OrderItems.Where(n => n.ProductId == product.ProductId
                                                        && n.Order.Date.Value.Month == month
                                                        && n.Order.Date.Value.Year == year
                                                        && n.Order.IsPay == 1
                                                        && n.Order.IsDeleted == 0);
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
                                  join s2 in _context.Products
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
                                      join s2 in _context.Products
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
            int totalOrders = _context.Orders.Where(n => n.Date.Value.Year == year).Count();
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
    }
}
