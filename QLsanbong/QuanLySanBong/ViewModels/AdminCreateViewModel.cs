namespace QuanLySanBong.ViewModels;

public class AdminCreateViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsEdit { get; set; }
    public int RecordId { get; set; }
    public List<AdminCreateField> Fields { get; set; } = new();
}

public class AdminCreateField
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    public string? Value { get; set; }
    public List<(string Value, string Text)> Options { get; set; } = new();
}
