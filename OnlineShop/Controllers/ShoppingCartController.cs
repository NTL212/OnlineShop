using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;



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
		public IActionResult AddToCart(int productId, int? count)
		{
			try
			{
				CartItem cartItem = _context.CartItems.SingleOrDefault(ci => ci.ProductId == productId && ci.IsDeleted == 0);

				if (cartItem != null)
				{
					// If it exists, update the quantity
					if (count.HasValue)
					{
						cartItem.Count = count.Value;
					}
					else
					{
						cartItem.Count++;
					}
				}
				else
				{
					// If it doesn't exist, create a new cart item
					Product product = _context.Products.SingleOrDefault(p => p.ProductId == productId);

					cartItem = new CartItem
					{
						ProductId = productId,
						Count = count.HasValue ? count.Value : 1,
						Date = DateTime.Now,
						IsDeleted = 0, // Assuming 0 means not deleted
						Product = product
					};

					_context.CartItems.Add(cartItem);
				}

				_context.SaveChanges();

				TempData["SuccessMessage"] = "Product added to cart successfully.";

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding product to cart.");
				TempData["ErrorMessage"] = "Error adding the product to the cart.";
				return RedirectToAction("Index");
			}
		}

		[HttpPost]
		public ActionResult Remove(int productId)
		{
			try
			{
				CartItem cartItem = _context.CartItems.SingleOrDefault(ci => ci.ProductId == productId && ci.IsDeleted == 0);

				if (cartItem != null)
				{
					// Instead of removing, you may want to mark it as deleted
					cartItem.IsDeleted = 1;
					_context.SaveChanges();
				}

				TempData["SuccessMessage"] = "Product removed from cart successfully.";

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing product from cart.");
				TempData["ErrorMessage"] = "Error removing the product from the cart.";
				return RedirectToAction("Index");
			}
		}

		[Route("cart.html", Name = "Cart")]
		public IActionResult Index()
		{
			// Retrieve non-deleted cart items from the database
			List<CartItem> cartItems = _context.CartItems
				.Include(ci => ci.Product)
				.Where(ci => ci.IsDeleted == 0)
				.ToList();

			return View(cartItems);
		}
	}

}
