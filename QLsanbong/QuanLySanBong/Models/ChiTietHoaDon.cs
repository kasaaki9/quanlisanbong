using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class ChiTietHoaDon
{
    [Key] public int MaChiTiet { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public int MaHoaDon { get; set; }
    public int MaDichVu { get; set; }

    public HoaDon? HoaDon { get; set; }
    public DichVu? DichVu { get; set; }
}