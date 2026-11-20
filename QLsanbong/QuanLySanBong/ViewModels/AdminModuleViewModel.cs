namespace QuanLySanBong.ViewModels;

public class AdminModuleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<AdminModuleRow> Rows { get; set; } = new();
}

public class AdminModuleRow
{
    public string Key { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}
