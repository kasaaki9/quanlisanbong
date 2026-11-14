namespace QuanLySanBong.ViewModels;

public class HomeIndexViewModel
{
    public List<LoaiSanHomeViewModel> NhomSan { get; set; } = new();
}

public class LoaiSanHomeViewModel
{
    public int SoNguoi { get; set; }
    public string TenNhom { get; set; } = string.Empty;
    public string AnhMacDinh { get; set; } = string.Empty;
    public List<SanBongHomeViewModel> SanBongs { get; set; } = new();
}

public class SanBongHomeViewModel
{
    public int MaSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string? TenLoaiSan { get; set; }
    public int SoLuongToiDa { get; set; }
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public decimal? GiaTu { get; set; }
}