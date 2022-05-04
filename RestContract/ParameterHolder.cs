namespace Net.Leksi.RestContract;

public class ParameterHolder
{
    public string Name { get; set; } = null!;
    public List<AttributeHolder> Attributes { get; init; } = new();

    public TypeHolder TypeHolder { get; set; } = null!;

    public override string ToString()
    {
        return ToString(false, false);
    }
    public string ToString(bool source, bool attributes)
    {
        return (attributes && Attributes.Count > 0 ? String.Join(Constants.Comma, Attributes) + Constants.Space : String.Empty) 
            + (source && TypeHolder.Source is { } ? TypeHolder.Source.ToString() : TypeHolder.ToString())
            + (Name is null ? String.Empty : Constants.Space + Name);
    }
}

