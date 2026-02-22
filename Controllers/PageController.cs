using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers
{
    // Controller này không có [ApiController] để trả về View được
    public class PageController : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login()
        {
            // Trả về file tại Views/Account/Login.cshtml
            return View("~/Views/Account/Login.cshtml");
        }
    }
}