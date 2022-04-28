namespace Net.Leksi.RestContract;

internal class ParameterHolder
{
    internal string Name { get; set; } = null!;
    internal List<AttributeHolder> Attributes { get; init; } = new();

    internal TypeHolder TypeHolder { get; set; } = null!;

    public override string ToString()
    {
        return ToString(false);
    }
    public string ToString(bool source)
    {
        return (Attributes.Count > 0 ? String.Join(Constants.Comma, Attributes) + Constants.Space : String.Empty) 
            + (source && TypeHolder.Source is { } ? TypeHolder.Source.ToString() : TypeHolder.ToString())
            + (Name is null ? String.Empty : Constants.Space + Name);
    }
}

