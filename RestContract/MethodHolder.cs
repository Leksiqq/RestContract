using System.Text.RegularExpressions;

namespace Net.Leksi.RestContract;

internal class MethodHolder
{
    internal string Name { get; set; } = null!;
    internal TypeHolder ReturnType { get; set; } = null!;
    internal List<ParameterHolder> Parameters { get; init; } = new();
    internal List<AttributeHolder> Attributes { get; init; } = new();
    internal string Path { get; set; } = null!;
    internal string HttpMethod { get; set; } = null!;
    internal Dictionary<string, string> RouteMatch { get; init; } = new();

    public override string ToString()
    {
        return ToString(false, true);
    }

    public string ToString(bool sources, bool returnType)
    {
        return (Attributes.Count > 0 ? String.Join(Constants.Comma, Attributes.Select(v => v.ToString())) + Constants.Space : String.Empty)
            + (returnType ? ReturnType.ToString() : String.Empty) + Constants.Space + Name + Constants.LeftParen 
            + string.Join(Constants.Comma, Parameters.Where(v => RouteMatch.ContainsKey(v.Name)).Select(v => v.ToString(sources))) + Constants.RightParen;
    }
}
