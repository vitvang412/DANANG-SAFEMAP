using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models.Entities
{
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tóm tắt")]
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; } // 1: Cảnh báo, 2: Tìm người

        public int Status { get; set; } = 1; // 1: Hiện, 0: Ẩn

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // BỔ SUNG: Để hết lỗi CS1061 trong Service (Update)
        public DateTime? UpdatedAt { get; set; }

        // BỔ SUNG: Để hết lỗi CS1061 trong Controller (Create)
        public int AuthorId { get; set; }

        // BỔ SUNG: Thuộc tính điều hướng để hết lỗi .Include(a => a.Author)
        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; } 
        public bool IsFeatured { get; set; } = false;
    }
}