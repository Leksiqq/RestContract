namespace Net.Leksi.RestContract;

public class MethodModel
{
    public string Type { get; set; }
    public string Name { get; set; }
    public List<AttributeModel> Attributes { get; set; }
    public List<ParameterModel> Parameters { get; set; }
}
