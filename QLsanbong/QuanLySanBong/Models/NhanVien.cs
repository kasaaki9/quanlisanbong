using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class NhanVien
{
    [Key] public int MaNhanVien { get; set; }
    [Required, StringLength(100)] public string HoTen { get; set; } = string.Empty;
    [StringLength(15)] public string? SoDienThoai { get; set; }
    [StringLength(100)] public string? Email { get; set; }
    [StringLength(50)] public string? ChucVu { get; set; }
    public bool TrangThai { get; set; } = true;

    public ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    public ICollection<CaLamViec> CaLamViecs { get; set; } = new List<CaLamViec>();
    public ICollection<LichLam> LichLams { get; set; } = new List<LichLam>();
}