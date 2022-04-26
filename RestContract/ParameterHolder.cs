namespace Net.Leksi.Server.Contract;

internal class ParameterHolder
{
    internal string Name { get; set; } = null!;
    internal List<AttributeHolder> Attributes { get; init; } = new();

    internal TypeHolder TypeHolder { get; set; } = null!;

    public override string ToString()
    {
        return (Attributes.Count > 0 ? String.Join(Constants.Comma, Attributes) + Constants.Space : String.Empty) + TypeHolder.ToString()
            + (Name is null ? String.Empty : Constants.Space + Name);
    }
}

