using System.Text.RegularExpressions;

namespace Net.Leksi.RestContract;

internal class MethodHolder
{
    internal string Name { get; set; } = null!;
    internal TypeHolder ReturnType { get; set; } = null!;
    internal List<ParameterHolder> Parameters { get; init; } = new();
    internal List<AttributeHolder> Attributes { get; init; } = new();
    internal string? Path { get; set; } = null;
    internal string? Query { get; set; } = null;
    internal string HttpMethod { get; set; } = null!;
    internal Dictionary<string, string>? PathMatch { get; set; } = null;
    internal Dictionary<string, string>? QueryMatch { get; set; } = null;

    public override string ToString()
    {
        return ToString(false, true);
    }

    public string ToString(bool sources, bool returnType)
    {
        return (returnType ? ReturnType.ToString() : String.Empty) + Constants.Space + Name + Constants.LeftParen 
            + string.Join(Constants.Comma, Parameters.Select(v => v.ToString(sources, false))) + Constants.RightParen;
    }
}
