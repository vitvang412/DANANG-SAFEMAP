using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers
{
    /// <summary>
    /// Controller trả View bản đồ — trang /Map.
    /// </summary>
    public class MapController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
