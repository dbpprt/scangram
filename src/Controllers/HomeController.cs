using Microsoft.AspNetCore.Mvc;

namespace Scangram.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
