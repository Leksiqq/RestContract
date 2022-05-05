using Microsoft.AspNetCore.Mvc;

namespace Net.Leksi.RestContract.Pages;

public class ConnectorBaseModel : BasePageModel
{
    public void OnGet([FromServices] IConnectorBaseBuilder builder)
    {
        builder.BuildConnectorBase(this);
    }
}
