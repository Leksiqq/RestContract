namespace Net.Leksi.RestContract;

internal class AttributeModel
{
    internal string Name { get; set; }
    internal Dictionary<string, string> Properties { get; init; } = new();
}
