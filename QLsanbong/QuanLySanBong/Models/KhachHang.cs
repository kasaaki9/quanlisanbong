using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class KhachHang
{
    [Key] public int MaKhachHang { get; set; }
    [Required, StringLength(100)] public string HoTen { get; set; } = string.Empty;
    [StringLength(15)] public string? SoDienThoai { get; set; }
    [StringLength(100)] public string? Email { get; set; }
    [StringLength(255)] public string? DiaChi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public bool TrangThai { get; set; } = true;

    public ICollection<PhieuDatSan> PhieuDatSans { get; set; } = new List<PhieuDatSan>();
}