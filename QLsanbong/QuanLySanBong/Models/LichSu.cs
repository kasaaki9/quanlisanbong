using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class LichSu
{
    [Key] public int MaLichSu { get; set; }
    public int MaNguoiDung { get; set; }
    public int MaLoaiTT { get; set; }
    [Required, StringLength(100)] public string HanhDong { get; set; } = string.Empty;
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    [StringLength(500)] public string? MoTa { get; set; }

    public NguoiDung? NguoiDung { get; set; }
    public LoaiThanhToan? LoaiThanhToan { get; set; }
}