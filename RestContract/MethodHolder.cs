namespace Net.Leksi.Server.Contract;

internal class MethodHolder
{
    internal string Name { get; set; } = null!;
    internal TypeHolder ReturnType { get; set; } = null!;
    internal List<ParameterHolder> Parameters { get; init; } = new();
    internal List<AttributeHolder> Attributes { get; init; } = new();
    internal List<string> Paths { get; init; } = new();
    internal List<string> HttpMethods { get; init; } = new();
    public override string ToString()
    {
        return (Attributes.Count > 0 ? String.Join(Constants.Comma, Attributes.Select(v => v.ToString())) + Constants.Space : String.Empty)
            + ReturnType.ToString() + Constants.Space + Name + Constants.LeftParen + string.Join(Constants.Comma, Parameters.Select(v => v.ToString())) + Constants.RightParen;
    }
}
