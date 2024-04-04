using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using OnlineShop.ViewModels;
using X.PagedList;

namespace OnlineShop.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class SellerOrdersController : Controller
    {
        private readonly OnlineShopContext _context;

        public SellerOrdersController(OnlineShopContext context)
        {
            _context = context;
        }

        // GET: Seller/SellerOrders
        public async Task<IActionResult> Index(int? page)
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
            var onlineShopContext = (from orderItem in _context.OrderItems
                                    join order in _context.Orders on orderItem.OrderId equals order.OrderId
                                    join product in _context.Products on orderItem.ProductId equals product.ProductId
                                    where product.SellerId == userId
                                    select order).Distinct().Include(o => o.Status).Include(o => o.User).OrderByDescending(o => o.Date);
            return View(onlineShopContext.ToPagedList(page ?? 1, 5));
        }

        // GET: Seller/SellerOrders/Details/5
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

            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            var query = from s1 in _context.OrderItems.Where(s1 => s1.OrderId == id && s1.Product.SellerId == userId)
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

        // GET: Seller/SellerOrders/Create
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
            ViewData["StatusId"] = new SelectList(_context.StatusOrders, "StatusId", "StatusName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Address");
            return View();
        }

        // POST: Seller/SellerOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,UserId,Receiver,ShipperId,Address,Phone,StatusId,IsPay,Email,Date,IsDeleted")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ShipperId"] = new SelectList(_context.Users, "UserId", "Address", order.ShipperId);
            ViewData["StatusId"] = new SelectList(_context.StatusOrders, "StatusId", "StatusName", order.StatusId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Address", order.UserId);
            return View(order);
        }

        // GET: Seller/SellerOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            int userId;
            string roleName = HttpContext.Session.GetString("roleName");
            bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
            if   (!isNum)
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

        // POST: Seller/SellerOrders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            order.StatusId = 2;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Seller/SellerOrders/Delete/5
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

        // POST: Seller/SellerOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
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

        [HttpPost, ActionName("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            order.StatusId = 3;
            order.IsPay = 1;
            _context.Update(order);
            await _context.SaveChangesAsync();

            var orderItems = _context.OrderItems.Where(o => o.OrderId == id).ToList();

            foreach (var item in orderItems)
            {
                Product product = _context.Products.SingleOrDefault(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    product.Sold += item.Count;
                    _context.Update(product);
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
