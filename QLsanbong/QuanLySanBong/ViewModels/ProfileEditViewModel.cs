using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.ViewModels;

public class ProfileEditViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    public string TenDangNhap { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string? Email { get; set; }

    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại cần có 10 chữ số và bắt đầu bằng số 0.")]
    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [DataType(DataType.Password)]
    public string? MatKhauHienTai { get; set; }

    [StringLength(64, ErrorMessage = "Mật khẩu mới tối đa 64 ký tự.")]
    [DataType(DataType.Password)]
    public string? MatKhauMoi { get; set; }

    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    public string? XacNhanMatKhauMoi { get; set; }
}