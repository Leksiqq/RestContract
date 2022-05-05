namespace Net.Leksi.RestContract;

internal class ParameterHolder
{
    internal string Name { get; set; } = null!;
    internal List<AttributeHolder> Attributes { get; init; } = new();

    internal TypeHolder TypeHolder { get; set; } = null!;

}

