namespace Net.Leksi.RestContract;

public class ContentHolder
{
    public string? Filename { get; set; }
    public string? FilenameParameter { get; set; }
    public Type? ContentType { get; set; }
    public string? ContentTypeParameter { get; set; }
    public string? ValueParameter { get; set; }
    public object? Value { get; set; }

    public List<ContentHolder>? Parts { get; set; }

}
