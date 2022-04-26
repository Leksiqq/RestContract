using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.Dto;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Базовый класс прокси котроллера API ASP.NET сервера с MVC, работающего в манере "коннектор-контроллер"
/// </para>
/// <para xml:lang="en">
/// The base class of the ASP.NET server API proxy controller with MVC, working in the "connector-controller" manner
/// </para>
/// </summary>
public class BaseControllerProxy: Controller
{
    private static readonly Regex _routeParams = new Regex("^(?:.*?{\\*?([^?:=}]+).*?)*$");

    private readonly DtoServiceProvider? _dtoServices;
    private readonly ILogger? _logger;

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализирует сервис-провайдером для работы с внедрёнными зависимостями.
    /// Проверяет совместимость контроллера с коннектором
    /// </para>
    /// <para xml:lang="en">
    /// Initializes with a service provider to work with injected dependencies
    /// Checks if the controller is compatible with the connector
    /// </para>
    /// </summary>
    /// <param name="services"></param>
    public BaseControllerProxy(IServiceProvider? services)
    {
        if (services is { })
        {
            if (services is DtoServiceProvider dtoServices)
            {
                _dtoServices = dtoServices;
            }
            else
            {
                _dtoServices = services.GetService<DtoServiceProvider>();
            }
            _logger = (ILogger?)services!.GetService(typeof(ILogger<BaseControllerProxy>));
        }
        else
        {
            _dtoServices = null;
            _logger = null;
        }

        ConnectorAttribute? connectorAttribute = null;
        object[] attributes = GetType().GetCustomAttributes(typeof(ConnectorAttribute), false);
        foreach(var attribute in attributes)
        {
            if(attribute.GetType() == typeof(ConnectorAttribute))
            {
                connectorAttribute = (ConnectorAttribute)attribute;
                break;
            }
        }
        if(connectorAttribute is { })
        {
            Type connectorType = connectorAttribute.Type;
            Type controllerType = GetType();
            foreach (MethodInfo connectorMethod in connectorType.GetMethods())
            {
                IEnumerable<RoutePathAttribute> routeAttributes = connectorMethod.GetCustomAttributes<RoutePathAttribute>();
                if (routeAttributes.Count() > 0)
                {
                    string[] paths = routeAttributes.Select(attr => attr.Path).ToArray();
                    string pathsString = string.Join(Constants.Comma, paths);
                    string[] httpMethods = connectorMethod.GetCustomAttributes<HttpMethodAttribute>().Select(attr => Utility.GetHttpMethodFromAttribute(attr)).ToArray();
                    List<string> acceptedPaths = new();


                    Type[] argTypes = connectorMethod.GetParameters()
                        .Where(p => p.GetCustomAttribute<NotParameterAttribute>() is null).Select(p =>
                        {
                            Type res = p.ParameterType;
                            if (_dtoServices?.IsRegistered(res) ?? false)
                            {
                                res = typeof(string);
                            }
                            return res;
                        }).ToArray();

                    MethodInfo? controllerMethod = controllerType.GetMethod(connectorMethod.Name, argTypes);
                    Type returnType = connectorMethod.ReturnType;
                    if (returnType != typeof(Task) && (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>)))
                    {
                        if (returnType == typeof(void))
                        {
                            returnType = typeof(Task);
                        }
                        else
                        {
                            returnType = typeof(Task<>).MakeGenericType(new[] { returnType });
                        }
                    }
                    if (controllerMethod is null || controllerMethod.ReturnType != returnType)
                    {
                        _logger?.LogWarning(@$"{controllerType} must contain method: 
{connectorMethod.ReturnType
                            } {connectorMethod.Name
                            }({string.Join(",", argTypes.Select(t => t.ToString()))})
Route(s) {pathsString} will be ignored");
                        continue;
                    }
                    string[] controllerPaths = controllerMethod.GetCustomAttributes<RouteAttribute>().Select(attr => attr.Template).ToArray();
                    foreach (string path in paths)
                    {
                        Match match = _routeParams.Match(path);
                        if (match.Success)
                        {
                            string[] missedRouteParameters = match.Groups[1].Captures.Select(c => c.Value)
                                .Where(v => !controllerMethod.GetParameters().Select(p => p.Name).Contains(v)).ToArray();
                            if (missedRouteParameters.Length > 0)
                            {
                                string missedParameters = string.Join(", ", missedRouteParameters);
                                _logger?.LogWarning($@"Not all route parameters from {path} matches by name 
{controllerMethod} parameters: {missedParameters}
Route {path} will be ignored");
                            }
                        }
                        if (!controllerPaths.Contains(path))
                        {
                            _logger?.LogWarning($@"Route {path} is not matches 
{controllerMethod} routes
Route {path} will be ignored");
                        }
                    }
                    AuthorizationAttribute? attr1 = connectorMethod.GetCustomAttribute<AuthorizationAttribute>();
                    AuthorizeAttribute? attr2 = controllerMethod.GetCustomAttribute<AuthorizeAttribute>();
                    if (
                        attr1 is { }
                        && (
                            attr2 is null || attr1.Policy != attr2.Policy
                            || attr1.AuthenticationSchemes != attr2.AuthenticationSchemes || attr1.Roles != attr2.Roles)
                    )
                    {
                        string message = @$"{controllerType} method {controllerMethod} 
must be marked with attribute: {typeof(AuthorizeAttribute)}({
                            string.Join(", ", typeof(AuthorizationAttribute).GetProperties()
                                .Where(p => p.Name != "TypeId" && p.GetValue(attr1) is { }).Select(p => $"{p.Name}={p.GetValue(attr1)}"))
                            })
Route(s) {pathsString} will be ignored";
                        _logger?.LogWarning(message);
                    }
                }
            }
        }
    }
}
