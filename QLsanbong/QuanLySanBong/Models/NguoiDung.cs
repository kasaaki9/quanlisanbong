using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class NguoiDung
{
    [Key] public int MaNguoiDung { get; set; }
    [Required, StringLength(50)] public string TenDangNhap { get; set; } = string.Empty;
    [Required, StringLength(255)] public string MatKhau { get; set; } = string.Empty;
    [Required, StringLength(100)] public string HoTen { get; set; } = string.Empty;
    [StringLength(15)] public string? SoDienThoai { get; set; }
    [StringLength(100)] public string? Email { get; set; }
    public bool TrangThai { get; set; } = true;
    public int MaVaiTro { get; set; }

    public VaiTro? VaiTro { get; set; }
    public ICollection<LichSu> LichSus { get; set; } = new List<LichSu>();
    public ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
    public ICollection<HoTro> HoTros { get; set; } = new List<HoTro>();
}