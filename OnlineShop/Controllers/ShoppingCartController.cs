using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using OnlineShop.ViewModels;

namespace OnlineShop.Controllers
{
	public class ShoppingCartController : Controller
	{
		private readonly OnlineShopContext _context;
		private readonly ILogger<ShoppingCartController> _logger;

		public ShoppingCartController(OnlineShopContext context, ILogger<ShoppingCartController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult AddToCart(int productId, int count)
		{
			if(count <= 0)
            {
				count = 1;
            }
			if(count > _context.Products.FirstOrDefault(n => n.ProductId == productId).Quantity)
            {
				string mess = "Thêm vào giỏ hàng thất bại";
				return RedirectToAction("Detail", "Product", new { id = productId, mess = mess });
            }
			Product product = _context.Products.FirstOrDefault(n => n.ProductId == productId);
			product.Quantity -= count;
			_context.Update(product);
			int userId;
			bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
			if (!isNum)
			{
				return RedirectToAction("SignIn", "Customer", new { area = "Default" });
			}
			ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
			try
			{
				CartItem cartItem = _context.CartItems.FirstOrDefault(n => n.ProductId == productId && n.IsDeleted == 0 && n.Cart.UserId == userId);

				if (cartItem != null)
				{
					cartItem.Count += count;
					_context.Update(cartItem);
				}
				else
				{
					int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
					cartItem = new CartItem
					{
						CartId = cartId,
						ProductId = productId,
						Count = count,
						Date = DateTime.Now,
						IsDeleted = 0
					};
					_context.CartItems.Add(cartItem);
				}
				_context.SaveChangesAsync();
				return RedirectToAction("Index", "Product");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding product to cart.");
				TempData["ErrorMessage"] = "Error adding the product to the cart.";
				return RedirectToAction("Index");
			}
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
			if(roleName != "Customer")
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
			List<OrderCartViewModel> cartItems = query.ToList();
			ViewBag.quantity = cartItems.Count;
			ViewBag.cartItems = cartItems;
			ViewBag.totalCartItems = cartItems.Sum(n => n.Total);
			return View();
		}

		[HttpPost, ActionName("Remove")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Remove(int id)
		{
			CartItem cartItems = _context.CartItems.FirstOrDefault(n => n.CartItemId == id);
			Product product = _context.Products.FirstOrDefault(n => n.ProductId == cartItems.ProductId);
			product.Quantity += cartItems.Count;
			_context.Products.Update(product);
			_context.CartItems.Remove(cartItems);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}
		public IActionResult OrderCart()
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
			User user = _context.Users.Where(n => n.UserId == userId).FirstOrDefault();
			ViewBag.username = user.UserName;
			int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
			ViewBag.quantity = _context.CartItems.Where(n => n.CartId == cartId).Count();
			ViewBag.cartItems = cartItems;
			ViewBag.totalCartItems = cartItems.Sum(n => n.Total);
			return View(user);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> OrderCart(string receiver, string email, string phone, string address)
        {
			int userId = int.Parse(HttpContext.Session.GetString("userId"));
			if (receiver == null || email == null | phone == null || address == null)
			{
				ViewBag.mess = "Vui lòng điền đầy đủ thông tin trước khi đặt hàng";
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
				User user = _context.Users.Where(n => n.UserId == userId).FirstOrDefault();
				ViewBag.username = user.UserName;
				int cartId = _context.Carts.FirstOrDefault(n => n.UserId == userId).CartId;
				ViewBag.quantity = _context.CartItems.Where(n => n.CartId == cartId).Count();
				ViewBag.cartItems = cartItems;
				ViewBag.totalCartItems = cartItems.Sum(n => n.Total);
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
			var lst = _context.CartItems.Where(n => n.Cart.UserId == userId && n.IsDeleted == 0).ToList();
			foreach(CartItem item in lst)
            {
				OrderItem orderItem = new OrderItem
				{
					OrderId = newOrderId,
					ProductId = item.ProductId,
					Count = item.Count,
				};
				_context.OrderItems.Add(orderItem);
				_context.CartItems.Remove(item);
				await _context.SaveChangesAsync();
			}
			return RedirectToAction("Index", "Product");
		}
	}
}
