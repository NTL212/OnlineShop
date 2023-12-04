using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;

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
        public IActionResult Index()
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
                return RedirectToAction("Index", "Product", new { area = "Default" });
            }
            ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
            int year = DateTime.Now.Year;
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
            if(growth(year) == 0)
            {
                ViewBag.growth = "0";
            }
            else 
            {
                ViewBag.growth = string.Format("{0:0.00}", growth(year));
            }
            ViewBag.totalUsers = totalUsers();
            ViewBag.totalOrders = totalOrders();
            List<string> monthlyRevenueLst = new List<string>();
            for (int i = 1; i <=12; i++)
            {
                if(monthlyRevenue(i, year) == 0 && i <= month)
                {
                    monthlyRevenueLst.Add("0 VNĐ");
                }   
                else if(monthlyRevenue(i, year) == 0 && i > month)
                {
                    monthlyRevenueLst.Add("Chưa có số liệu");
                }
                else
                {
                    monthlyRevenueLst.Add(string.Format("{0:0,0} VNĐ", monthlyRevenue(i, year)));
                }
            }
            ViewBag.monthlyRevenue = monthlyRevenueLst;
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
        public int totalUsers()
        {
            int totalUser = _context.Users.Count();
            return totalUser;
        }
        public int totalOrders()
        {
            int totalOrders = _context.Orders.Count();
            return totalOrders;
        }
        public decimal growth(int year)
        {
            decimal lastYear = totalRevenue(year - 1);
            decimal theYearBeforeLast = totalRevenue(year - 2);
            decimal growth = (lastYear - theYearBeforeLast) / theYearBeforeLast * 100;
            return growth;
        }
    }
}
