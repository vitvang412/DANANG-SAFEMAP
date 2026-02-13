using System.ComponentModel.DataAnnotations;

namespace DaNangSafeMap.Models
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã xác nhận")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác nhận phải có 6 số")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã xác nhận phải là 6 chữ số")]
        public string Otp { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
