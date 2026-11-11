using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class DichVu
{
    [Key] public int MaDichVu { get; set; }
    [Required, StringLength(100)] public string TenDichVu { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    [StringLength(20)] public string? DonViTinh { get; set; }
    public int SoLuongTon { get; set; }

    public ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}