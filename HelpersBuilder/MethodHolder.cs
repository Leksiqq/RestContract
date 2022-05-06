using System.Text.RegularExpressions;

namespace Net.Leksi.RestContract;

internal class MethodHolder
{
    internal string Name { get; set; } = null!;
    internal TypeHolder ReturnType { get; set; } = null!;
    internal List<ParameterHolder> Parameters { get; init; } = new();
    internal List<AttributeHolder> Attributes { get; init; } = new();
    internal string? Path { get; set; } = null;
    internal string HttpMethod { get; set; } = "Get";
    internal Dictionary<string, string>? PathMatch { get; set; } = null;
    internal Dictionary<string, string>? QueryMatch { get; set; } = null;

}
