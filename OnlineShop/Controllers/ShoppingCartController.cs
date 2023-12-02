using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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
			int userId;
			bool isNum = int.TryParse(HttpContext.Session.GetString("userId"), out userId);
			if (!isNum)
			{
				return RedirectToAction("SignIn", "Customer", new { area = "Default" });
			}
			ViewBag.username = _context.Users.Where(n => n.UserId == userId).FirstOrDefault().UserName;
			try
			{
				CartItem cartItem = _context.CartItems.FirstOrDefault(n => n.ProductId == productId && n.IsDeleted == 0);

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
				_context.SaveChanges();
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
			// Retrieve non-deleted cart items from the database
			List<CartItem> cartItems = _context.CartItems
				.Include(n => n.Product)
				.Where(n => n.IsDeleted == 0 && n.Cart.UserId == userId)
				.ToList();

			return View(cartItems);
		}

		[HttpPost, ActionName("Remove")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Remove(int id)
		{
			CartItem cartItems = _context.CartItems.FirstOrDefault(n => n.CartItemId == id);
			_context.CartItems.Remove(cartItems);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}
		public IActionResult Payment()
		{
			return View();
		}

	}
}
