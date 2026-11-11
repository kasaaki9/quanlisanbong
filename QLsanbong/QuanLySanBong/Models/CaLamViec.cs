using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class CaLamViec
{
    [Key] public int MaCa { get; set; }
    [Required, StringLength(50)] public string TenCa { get; set; } = string.Empty;
    public TimeSpan ThoiGianBatDau { get; set; }
    public TimeSpan ThoiGianKetThuc { get; set; }
    [StringLength(20)] public string? ThuTrongTuan { get; set; }
    public int MaNhanVien { get; set; }

    public NhanVien? NhanVien { get; set; }
    public ICollection<LichLam> LichLams { get; set; } = new List<LichLam>();
}