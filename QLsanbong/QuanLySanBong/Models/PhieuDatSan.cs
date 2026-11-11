using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class PhieuDatSan
{
    [Key] public int MaPhieu { get; set; }
    public DateTime NgayDat { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    [StringLength(50)] public string TrangThai { get; set; } = "Chờ xác nhận";
    public decimal TongTienSan { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public int MaKhachHang { get; set; }
    public int MaSan { get; set; }

    public KhachHang? KhachHang { get; set; }
    public SanBong? SanBong { get; set; }
    public HoaDon? HoaDon { get; set; }
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
}