using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class LoaiThanhToan
{
    [Key] public int MaLoaiTT { get; set; }
    [Required, StringLength(50)] public string TenLoaiTT { get; set; } = string.Empty;

    public ICollection<LichSu> LichSus { get; set; } = new List<LichSu>();
}