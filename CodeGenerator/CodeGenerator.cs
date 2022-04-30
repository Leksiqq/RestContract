namespace Net.Leksi.RestContract;

public class CodeGenerator
{
    public void GenerateHelpers<TConnector>(string controllerInterfaceFullName, string controllerProxyFullName, 
        string connectorBaseFullName, Dictionary<string, string> target)
    {
        Server server = new();
        server.Generate<TConnector>(controllerInterfaceFullName, controllerProxyFullName, connectorBaseFullName, target);
    }
}
