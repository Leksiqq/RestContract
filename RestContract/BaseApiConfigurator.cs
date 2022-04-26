using Microsoft.AspNetCore.Authorization;
using Net.Leksi.Dto;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Базовый класс конфигуратора API ASP.NET сервера, работающего в манере "коннектор-API"
/// </para>
/// <para xml:lang="en">
/// The base class of the ASP.NET server API configurator that works in the "connector-API" manner
/// </para>
/// </summary>
public class BaseApiConfigurator
{
    private static readonly Regex _routeParams = new Regex("^(?:.*?{\\*?([^?:=}]+).*?)*$");

    private readonly DtoServiceProvider? _dtoServices;
    private readonly ILogger? _logger;

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализирует сервис-провайдером для работы с внедрёнными зависимостями
    /// </para>
    /// <para xml:lang="en">
    /// Initializes with a service provider to work with injected dependencies
    /// </para>
    /// </summary>
    /// <param name="services"></param>
    public BaseApiConfigurator(IServiceProvider? services)
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
            _logger = (ILogger?)services!.GetService(typeof(ILogger<BaseApiConfigurator>));
        }
        else
        {
            _dtoServices = null;
            _logger = null;
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Связывает методы коннектора с методами API и создаёт RouteHandlers
    /// </para>
    /// <para xml:lang="en">
    /// Associates connector methods with API methods and creates RouteHandlers
    /// </para>
    /// </summary>
    /// <typeparam name="TIConnector">
    /// <para xml:lang="ru">
    /// Интерфейс коннектора
    /// </para>
    /// <para xml:lang="en">
    /// Connector interface
    /// </para>
    /// </typeparam>
    /// <typeparam name="TIApi">
    /// <para xml:lang="ru">
    /// Интерфейс API
    /// </para>
    /// <para xml:lang="en">
    /// API interface
    /// </para>
    /// </typeparam>
    /// <param name="app">
    /// <para xml:lang="ru">
    /// <see cref="IEndpointRouteBuilder"/>, например, <see cref="WebApplication"/>
    /// </para>
    /// <para xml:lang="en">
    /// <see cref="IEndpointRouteBuilder"/>, e.g. <see cref="WebApplication"/>
    /// </para>
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <para xml:lang="ru">
    /// Выбрасывается, если число параметров метода API превышает 9
    /// </para>
    /// <para xml:lang="en">
    /// Thrown if the number of API method parameters exceeds 9
    /// </para>
    /// </exception>
    public void Configure<TIConnector, TIApi>(IEndpointRouteBuilder app)
    {

        Type type = GetType();

        foreach (MethodInfo connectorMethod in typeof(TIConnector).GetMethods())
        {
            IEnumerable<RoutePathAttribute> routeAttributes = connectorMethod.GetCustomAttributes<RoutePathAttribute>();
            if (routeAttributes.Count() > 0)
            {
                string[] paths = routeAttributes.Select(attr => attr.Path).ToArray();
                string pathsString = string.Join(Constants.Comma, paths);
                string[] httpMethods = connectorMethod.GetCustomAttributes<HttpMethodAttribute>().Select(attr => Utility.GetHttpMethodFromAttribute(attr)).ToArray();
                List<string> acceptedPaths = new();


                Type[] argTypes = new Type[] { typeof(HttpContext) }.Concat(connectorMethod.GetParameters()
                    .Where(p => p.GetCustomAttribute<NotParameterAttribute>() is null).Select(p =>
                    {
                        Type res = p.ParameterType;
                        if (_dtoServices?.IsRegistered(res) ?? false)
                        {
                            res = typeof(string);
                        }
                        return res;
                    })).ToArray();

                MethodInfo? controllerMethod = typeof(TIApi).GetMethod(connectorMethod.Name, argTypes);
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
                    _logger?.LogWarning(@$"{typeof(TIApi)} must contain method: 
{connectorMethod.ReturnType
                        } {connectorMethod.Name
                        }({string.Join(",", argTypes.Select(t => t.ToString()))})
Route(s) {pathsString} will be ignored");
                    continue;
                }
                foreach(string path in paths)
                {
                    Match match = _routeParams.Match(path);
                    if (match.Success)
                    {
                        string[] missedRouteParameters = match.Groups[1].Captures.Select(c => c.Value)
                            .Where(v => !connectorMethod.GetParameters().Select(p => p.Name).Contains(v)).ToArray();
                        if (missedRouteParameters.Length > 0)
                        {
                            string missedParameters = string.Join(", ", missedRouteParameters);
                            _logger?.LogWarning($@"Not all route parameters from {path} matches by name 
{connectorMethod} parameters: {missedParameters}
Route {path} will be ignored");
                            continue;
                        }
                    }
                    acceptedPaths.Add(path);
                }
                Type apiPropertyType = null!;
                Type[] apiPropertyTypeParameters = argTypes.Concat(new Type[] { returnType }).ToArray();
                apiPropertyType = (apiPropertyTypeParameters.Length switch
                {
                    1 => typeof(Func<>),
                    2 => typeof(Func<,>),
                    3 => typeof(Func<,,>),
                    4 => typeof(Func<,,,>),
                    5 => typeof(Func<,,,,>),
                    6 => typeof(Func<,,,,,>),
                    7 => typeof(Func<,,,,,,>),
                    8 => typeof(Func<,,,,,,,>),
                    9 => typeof(Func<,,,,,,,,>),
                    _ => throw new InvalidOperationException()
                }).MakeGenericType(apiPropertyTypeParameters);
                PropertyInfo? apiProperty = type.GetProperty(connectorMethod.Name);
                if (apiProperty is null || apiProperty.PropertyType != apiPropertyType || apiProperty.GetValue(this) is null)
                {
                    _logger?.LogWarning(@$"{type} must contain not null property: {apiPropertyType} {connectorMethod.Name}
Route(s) {pathsString} will be ignored");
                    continue;
                }

                Delegate handler = (Delegate)apiProperty.GetValue(this)!;

                AuthorizationAttribute? attr1 = connectorMethod.GetCustomAttribute<AuthorizationAttribute>();
                AuthorizeAttribute? attr2 = handler.Method.GetCustomAttribute<AuthorizeAttribute>();
                if (
                    attr1 is { }
                    && (
                        attr2 is null || attr1.Policy != attr2.Policy
                        || attr1.AuthenticationSchemes != attr2.AuthenticationSchemes || attr1.Roles != attr2.Roles)
                )
                {
                    string message = @$"{type} property {apiPropertyType} {connectorMethod.Name
                        } value must be marked with attribute: {typeof(AuthorizeAttribute)}({
                        string.Join(", ", typeof(AuthorizationAttribute).GetProperties()
                            .Where(p => p.Name != "TypeId" && p.GetValue(attr1) is { }).Select(p => $"{p.Name}={p.GetValue(attr1)}"))
                        })
Route(s) {pathsString} will be ignored";
                    _logger?.LogWarning(message);
                    continue;
                }


                
                foreach(string path in acceptedPaths)
                {
                    app.MapMethods(path, httpMethods, handler);
                }

            }
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Вызов метода API из RouteHandler
    /// </para>
    /// <para xml:lang="en">
    /// Call API method from RouteHandler
    /// </para>    
    /// </summary>
    /// <typeparam name="TIApi">
    /// <para xml:lang="ru">
    /// Интерфейс API
    /// </para>
    /// <para xml:lang="en">
    /// API interface
    /// </para>    
    /// </typeparam>
    /// <param name="args">
    /// <para xml:lang="ru">
    /// Значения параметров метода API
    /// </para>    
    /// <para xml:lang="en">
    /// API method parameter values
    /// </para>
    /// </param>
    /// <param name="name">
    /// <para xml:lang="ru">
    /// Имя метода API
    /// </para>    
    /// <para xml:lang="en">
    /// API method name
    /// </para>
    /// </param>
    /// <returns>
    /// <para xml:lang="ru">
    /// Возвращаемое значение метода API
    /// </para>    
    /// <para xml:lang="en">
    /// API method return value
    /// </para>    
    /// </returns>
    protected object? CallApiMethod<TIApi>(object?[] args, [CallerMemberName] string name = "")
    {
        return typeof(TIApi).GetMethod(name)?.Invoke(((HttpContext)args[0]!).RequestServices.GetRequiredService(typeof(TIApi)), args);
    }

}

