namespace Net.Leksi.RestContract;

public class ContentAttribute: Attribute
{
    public int Part { get; init; }

    public ContentAttribute(int part)
    {
        Part = part;
    }
}
