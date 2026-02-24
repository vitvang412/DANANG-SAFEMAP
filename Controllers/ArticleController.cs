using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace DaNangSafeMap.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ArticleController(IArticleService articleService, IWebHostEnvironment hostEnvironment)
        {
            _articleService = articleService;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var articles = await _articleService.GetAllArticlesAsync();
            return View(articles);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null) return NotFound();
            return View(article);
        }

        // 3. Trang tạo bài viết (Bỏ Roles = "Admin" để User thường cũng vào được)
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Cảnh báo tội phạm" },
                new SelectListItem { Value = "2", Text = "Tìm kiếm người thân" },
                new SelectListItem { Value = "3", Text = "Tin tức cộng đồng" }
            };
            return View();
        }

        // 4. Xử lý tạo bài viết
        [Authorize] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Article article, IFormFile imageFile)
        {
            ModelState.Remove("AuthorId");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try 
                {
                    // --- XỬ LÝ UPLOAD ẢNH (Giữ nguyên logic của bạn) ---
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                        
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        string filePath = Path.Combine(uploadDir, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        article.ImageUrl = "/uploads/" + fileName;
                    }

                    // --- GÁN THÔNG TIN TÁC GIẢ & TRẠNG THÁI ---
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null)
                    {
                        article.AuthorId = int.Parse(userIdClaim.Value);
                        article.CreatedAt = DateTime.Now;
                        
                        // LOGIC KIỂM DUYỆT:
                        // Nếu là Admin thì cho hiện ngay (Status = 1)
                        // Nếu là User thường thì để chờ duyệt (Status = 0)
                        if (User.IsInRole("Admin")) 
                        {
                            article.Status = 1; 
                        } 
                        else 
                        {
                            article.Status = 0; 
                        }

                        var result = await _articleService.CreateArticleAsync(article);
                        if (result) 
                        {
                            TempData["Success"] = User.IsInRole("Admin") 
                                ? "Đăng bài viết thành công!" 
                                : "Gửi bài thành công! Tin tức của bạn đang chờ Admin kiểm duyệt.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            ViewBag.Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Cảnh báo tội phạm" },
                new SelectListItem { Value = "2", Text = "Tìm kiếm người thân" },
                new SelectListItem { Value = "3", Text = "Tin tức cộng đồng" }
            };
            return View(article);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _articleService.DeleteArticleAsync(id);
            if (result) TempData["Success"] = "Xóa bài thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}