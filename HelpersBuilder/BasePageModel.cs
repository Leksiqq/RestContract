using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Net.Leksi.RestContract;

public class BasePageModel: PageModel
{
    internal string NamespaceValue { get; set; }
    internal string ClassName { get; set; }
    internal List<string> Usings { get; set; }
    internal List<MethodModel> Methods { get; set; }
}
