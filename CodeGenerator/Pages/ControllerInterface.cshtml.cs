using Microsoft.AspNetCore.Mvc;

namespace Net.Leksi.RestContract.Pages;

public class ControllerInterfaceModel : BasePageModel
{
    public void OnGet([FromServices] IMvcControllerInterfaceBuilder builder)
    {
        builder.GenerateMvcControllerInterface(this);
    }
}
