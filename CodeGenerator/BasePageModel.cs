using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Net.Leksi.RestContract;

public class BasePageModel: PageModel
{
    public string NamespaceValue { get; set; }
    public string ClassName { get; set; }
    public List<string> Usings { get; set; }
    public List<MethodModel> Methods { get; set; }
}
