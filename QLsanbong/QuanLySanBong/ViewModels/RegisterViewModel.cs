using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng tạo tên đăng nhập.")]
        [StringLength(50, MinimumLength = 4,
            ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$",
            ErrorMessage = "Tên đăng nhập chỉ được dùng chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9}$",
            ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng tạo mật khẩu.")]
        [StringLength(64, MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải từ 8 đến 64 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).+$",
            ErrorMessage = "Mật khẩu cần có chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        [DataType(DataType.Password)]
        public string NhapLaiMatKhau { get; set; } = string.Empty;
    }
}