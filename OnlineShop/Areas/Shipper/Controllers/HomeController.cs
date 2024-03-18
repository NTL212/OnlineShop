using Microsoft.AspNetCore.Mvc;

namespace OnlineShop.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
