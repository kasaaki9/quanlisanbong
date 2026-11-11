using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class SanBong
{
    [Key] public int MaSan { get; set; }
    [Required, StringLength(100)] public string TenSan { get; set; } = string.Empty;
    public int MaLoaiSan { get; set; }
    [StringLength(500)] public string? MoTa { get; set; }
    [StringLength(255)] public string? HinhAnh { get; set; }
    [StringLength(50)] public string TrangThai { get; set; } = "Hoạt động";

    public LoaiSan? LoaiSan { get; set; }
    public ICollection<PhieuDatSan> PhieuDatSans { get; set; } = new List<PhieuDatSan>();
    public ICollection<BangGia> BangGias { get; set; } = new List<BangGia>();
}