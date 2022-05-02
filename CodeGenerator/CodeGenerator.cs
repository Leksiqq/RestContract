namespace Net.Leksi.RestContract;

public class CodeGenerator
{
    public void GenerateHelpers<TConnector>(string controllerInterfaceFullName, string controllerProxyFullName, 
        string connectorBaseFullName, Dictionary<string, string> target)
    {
        Requisitor requisitor = new(null);
        requisitor.FullName = controllerInterfaceFullName;
        Server server = new();
        server.Generate(requisitor, target);
    }
}
