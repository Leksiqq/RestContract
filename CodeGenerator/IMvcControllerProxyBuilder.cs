using Net.Leksi.RestContract.Pages;

namespace Net.Leksi.RestContract;

public interface IMvcControllerProxyBuilder
{
    void GenerateMvcControllerProxy(ControllerProxyModel model);
}
