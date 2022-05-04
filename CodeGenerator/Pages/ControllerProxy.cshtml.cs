using Microsoft.AspNetCore.Mvc;

namespace Net.Leksi.RestContract.Pages
{
    public class ControllerProxyModel : BasePageModel
    {
        public void OnGet([FromServices] IMvcControllerProxyBuilder builder)
        {
            builder.GenerateMvcControllerProxy(this);
        }
    }
}
