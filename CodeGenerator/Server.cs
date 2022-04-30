using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace Net.Leksi.RestContract;

internal class Server
{
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

            WebApplication app = builder.Build();

            string secretWord = Guid.NewGuid().ToString();

            app.Use(async (context, next) =>
            {
                if(!context.Request.Headers.ContainsKey("X-Secret-Word") || !context.Request.Headers["X-Secret-Word"].Contains(secretWord))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"X-Secret-Word: {secretWord}");
                }
                else
                {
                    await next.Invoke(context);
                }
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.MapRazorPages();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Console.WriteLine(string.Join("; ", app.Urls));
                Client client = new();
                client.Run<TConnector>(new Uri(app.Urls.First()), controllerInterfaceFullName, controllerProxyFullName, connectorBaseFullName, target);
                //app.StopAsync();
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
                ++port;
            }
        }

    }
}
