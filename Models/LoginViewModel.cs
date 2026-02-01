using System.ComponentModel.DataAnnotations;

namespace DaNangSafeMap.Models
{
    // Đổi tên class để tránh trùng lặp
    public class LoginViewModel  // Đổi từ LoginModel thành UserLoginModel
    {
        [Display(Name = "Email hoặc Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        public string Password { get; set; } = string.Empty; // Thêm giá trị mặc định

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; } = false;
    }
}