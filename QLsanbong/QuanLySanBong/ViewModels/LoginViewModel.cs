using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
        [StringLength(100, ErrorMessage = "Thông tin đăng nhập không được quá 100 ký tự.")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(64, MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải từ 8 đến 64 ký tự.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        public bool GhiNhoDangNhap { get; set; }
    }
}