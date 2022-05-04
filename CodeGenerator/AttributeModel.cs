namespace Net.Leksi.RestContract;

public class AttributeModel
{
    public string Name { get; set; }
    public Dictionary<string, string> Properties { get; init; } = new();
}
