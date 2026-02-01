using System.ComponentModel.DataAnnotations;

namespace DaNangSafeMap.Models
{
    public class EditProfileViewModel
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string Gender { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Display(Name = "Mật khẩu hiện tại")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu mới")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Display(Name = "Xác nhận mật khẩu mới")]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
