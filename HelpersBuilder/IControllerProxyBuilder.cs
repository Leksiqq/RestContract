using Net.Leksi.RestContract.Pages;

namespace Net.Leksi.RestContract;

public interface IControllerProxyBuilder
{
    void BuildControllerProxy(ControllerProxyModel model);
}
