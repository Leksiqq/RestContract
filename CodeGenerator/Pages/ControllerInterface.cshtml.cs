using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Net.Leksi.RestContract.Pages;

public class ControllerInterfaceModel : PageModel
{
    public string FullName { get; set; }
    public void OnGet()
    {
    }
}
