namespace Net.Leksi.RestContract;

[AttributeUsage(AttributeTargets.Parameter)]
public class FilenameAttribute: Attribute
{
    public string Filename { get; init; }

    public FilenameAttribute(string filename)
    {
        Filename = filename;
    }
}
