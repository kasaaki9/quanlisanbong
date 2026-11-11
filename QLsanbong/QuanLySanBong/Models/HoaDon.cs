using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class HoaDon
{
    [Key] public int MaHoaDon { get; set; }
    public int MaPhieu { get; set; }
    public int MaNhanVienLap { get; set; }
    public DateTime NgayLap { get; set; } = DateTime.Now;
    public decimal TongTienSan { get; set; }
    public decimal TongTienDichVu { get; set; }
    public decimal TongCong { get; set; }
    [StringLength(50)] public string? PhuongThucThanhToan { get; set; }
    public bool TrangThai { get; set; }

    public PhieuDatSan? PhieuDatSan { get; set; }
    public NhanVien? NhanVienLap { get; set; }
    public ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}