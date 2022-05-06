namespace Net.Leksi.RestContract;

internal class MethodModel
{
    internal string Type { get; set; }
    internal string Name { get; set; }
    internal List<AttributeModel> Attributes { get; set; }
    internal List<ParameterModel> Parameters { get; set; }
    internal bool HasSerialized { get; set; } = false;
    internal string GetOptionsVariable { get; set; }
    internal string GetConverterVariable { get; set; }
    internal List<Tuple<string, string, string>> Deserializing { get; set; }
    internal string ControllerInterfaceClassName { get; set; }
    internal string ControllerVariable { get; set; }
    internal List<string> ControllerParameters { get; init; } = new();
    internal string RouteVariable { get; set; }
    internal string RouteValue { get; set; }
    internal string HttpRequestVariable { get; set; }
    internal string HttpMethod { get; set; }
    internal string PostOptionsVariable { get; set; }
    internal string PostConverterVariable { get; set; }
    internal string BodyVariable { get; set; }
    internal string BodyType { get; set; }

}
