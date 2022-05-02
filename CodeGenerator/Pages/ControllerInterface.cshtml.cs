using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Net.Leksi.RestContract.Pages;

public class ControllerInterfaceModel : PageModel
{
    private readonly Requisitor _requisitor;
    public string FullName => _requisitor.FullName;

    public ControllerInterfaceModel(Requisitor requisitor)
    {
        _requisitor = requisitor;
    }
    public void OnGet()
    {
    }
}
