using Microsoft.AspNetCore.Mvc;

namespace Net.Leksi.RestContract.Pages;

public class ControllerInterfaceModel : BasePageModel
{
    public void OnGet([FromServices] IControllerInterfaceBuilder builder)
    {
        builder.BuildControllerInterface(this);
    }
}
