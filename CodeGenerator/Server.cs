using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.FileProviders;
using Net.Leksi.Dto;
using System.Reflection;

namespace Net.Leksi.RestContract;

internal class Server
{
    internal const string SecretWordHeader = "X-Secret-Word";
    internal void Generate<TConnector>(string controllerInterfaceFullName, string controllerProxyFullName, 
        string connectorBaseFullName, Dictionary<string, string> target)
    {
        string result = string.Empty;
        int port = 5000;
        while (true)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new string[] { });

            builder.Logging.ClearProviders();

            builder.Services.AddRazorPages();

            Assembly assembly = GetType().Assembly;

            builder.Services.AddControllersWithViews()
                .AddApplicationPart(assembly)
                .AddRazorRuntimeCompilation();

            builder.Services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            {
                options.FileProviders.Add(new EmbeddedFileProvider(assembly));
            });

            DtoKit.Install(builder.Services, services => { });

            builder.Services.AddSingleton<Requisitor>();

            WebApplication app = builder.Build();

            string secretWord = Guid.NewGuid().ToString();

            app.Use(async (context, next) =>
            {
                if(!context.Request.Headers.ContainsKey(SecretWordHeader) || !context.Request.Headers[SecretWordHeader].Contains(secretWord))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("");
                }
                else
                {
                    await next.Invoke(context);
                }
            });

            app.MapRazorPages();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                BuildModels<TConnector>(controllerInterfaceFullName, controllerProxyFullName, connectorBaseFullName);
                target["ControllerInterface"] = null;
                target["ControllerProxy"] = null;
                target["ConnectorBase"] = null;
                Client client = new();
                client.Run(new Uri(app.Urls.First()), secretWord, target);
                app.StopAsync();
            });

            app.Urls.Clear();
            app.Urls.Add($"http://localhost:{port}");
            try
            {
                app.Run();
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
                ++port;
            }
        }

    }

    private void BuildModels<TConnector>(string controllerInterfaceFullName, string controllerProxyFullName, string connectorBaseFullName)
    {
    }
}
