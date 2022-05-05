namespace Net.Leksi.RestContract;

internal class MethodModel
{
    internal string Type { get; set; }
    internal string Name { get; set; }
    internal List<AttributeModel> Attributes { get; set; }
    internal List<ParameterModel> Parameters { get; set; }
    internal bool HasSerialized { get; set; } = false;
    internal string OptionsVariable { get; set; }
    internal string ConverterVariable { get; set; }
    internal List<Tuple<string, string, string>> Deserializing { get; set; }
    internal string ControllerInterfaceClassName { get; set; }
    internal string ControllerVariable { get; set; }
    internal List<string> ControllerParameters { get; init; } = new();

}
