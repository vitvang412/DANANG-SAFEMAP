using DaNangSafeMap.Data;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DaNangSafeMap.Services.Implementations
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;

        public ArticleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Article>> GetAllArticlesAsync()
        {
            return await _context.Articles
                .Include(a => a.Author) // Hết lỗi vì Article đã có Author
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Article?> GetArticleByIdAsync(int id)
        {
            return await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<bool> CreateArticleAsync(Article article)
        {
            _context.Articles.Add(article);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateArticleAsync(Article article)
        {
            var existingArticle = await _context.Articles.FindAsync(article.Id);
            if (existingArticle == null) return false;

            existingArticle.Title = article.Title;
            existingArticle.Summary = article.Summary;
            existingArticle.Content = article.Content;
            existingArticle.ImageUrl = article.ImageUrl;
            existingArticle.Status = article.Status;
            existingArticle.CategoryId = article.CategoryId;
            existingArticle.UpdatedAt = DateTime.Now; // Hết lỗi UpdatedAt

            _context.Articles.Update(existingArticle);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            _context.Articles.Remove(article);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}