using DtoKit.Demo;
using Net.Leksi.RestContract;

CodeGenerator cg = new();
Dictionary<string, string> map = new();
cg.GenerateHelpers<IConnector>("DtoKit.Demo.IDemoController", "DtoKit.Demo.DemoControllerProxy", "DtoKit.Demo.DemoConnectorBase", map);
Console.WriteLine(string.Join("\n//--------------\n// Cut here\n//--------------\n", map.Values));
