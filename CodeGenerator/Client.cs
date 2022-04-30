namespace Net.Leksi.RestContract;

internal class Client
{
    internal void Run<TConnector>(Uri baseAddress, string controllerInterfaceFullName, string controllerProxyFullName, 
        string connectorBaseFullName, Dictionary<string, string> target)
    {
        target["controllerInterface"] = controllerInterfaceFullName;
        target["controllerProxy"] = controllerProxyFullName;
        target["connectorBase"] = connectorBaseFullName;
    }
}
