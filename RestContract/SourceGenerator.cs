using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.Dto;
using System.Reflection;
using System.Text;

namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Генерирует классы API конфигуратора или прокси контроллера, интерфейсы API или контроллера 
/// на основе класса коннектора
/// </para>
/// <para xml:lang="en">
/// Generates configurator or proxy controller API classes, API or controller interfaces
/// based on the connector class
/// </para>
/// </summary>
public partial class SourceGenerator
{
    private readonly DtoServiceProvider? _dtoServices;

    private List<MethodHolder> _methods = new();

    private string _namespace = null!;
    private string _class = null!;

    private string _anotherNamespace = null!;
    private string _anotherClass = null!;

    private readonly List<string> _usings = new();
    private readonly StringBuilder _sb = new();

    private Type _connectorType = null!;

    private readonly NamespaceComparer _namespaceComparer = new();

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализирует сервис-провайдером для работы с внедрёнными зависимостями.
    /// </para>
    /// <para xml:lang="en">
    /// Initializes with a service provider to work with injected dependencies
    /// </para>
    /// </summary>
    /// <param name="services"></param>
    public SourceGenerator(IServiceProvider? services)
    {
        if (services is DtoServiceProvider dtoServices)
        {
            _dtoServices = dtoServices;
        }
        else if (services is { })
        {
            _dtoServices = services.GetService<DtoServiceProvider>();
        }
        else
        {
            _dtoServices = null;
        }
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Генерирует интерфейс API и класс конфигуратора API на основе интерфейса коннектора
    /// </para>
    /// <para xml:lang="en">
    /// Generates an API interface and an API configurator class based on the connector interface
    /// </para>
    /// </summary>
    /// <typeparam name="TConnector"></typeparam>
    /// <param name="controllerFullName"></param>
    /// <param name="configuratorFullName"></param>
    /// <returns></returns>
    public string GenerateApiInterfaceAndConfiguratorClass<TConnector>(string controllerFullName, string configuratorFullName)
    {
        CollectRequisites<TConnector>(controllerFullName, configuratorFullName);
        return @$"//------------------------------
// Controller interface {controllerFullName} (Generated automatically)
//------------------------------
{GenerateApiInterface(false)}
//------------------------------
// Cut here
//------------------------------
//------------------------------
// Configurator class {configuratorFullName} (Generated automatically)
//------------------------------
{ GenerateApiConfiguratorClass()}
";
    }

    /// <summary>
    /// <para xml:lang="ru">
    /// Генерирует интерфейс контроллера MVC и класс прокси контроллера MVC на основе интерфейса коннектора
    /// </para>
    /// <para xml:lang="en">
    /// Generates an MVC controller interface and an MVC controller proxy class based on the connector interface
    /// </para>
    /// </summary>
    /// <typeparam name="TConnector"></typeparam>
    /// <param name="controllerFullName"></param>
    /// <param name="proxyFullName"></param>
    /// <returns></returns>
    public string GenerateMvcControllerInterfaceAndProxyClass<TConnector>(string controllerFullName, string proxyFullName)
    {
        CollectRequisites<TConnector>(controllerFullName, proxyFullName);
        return @$"//------------------------------
// MVC Controller interface {controllerFullName} (Generated automatically)
//------------------------------
{GenerateApiInterface(true)}
//------------------------------
// Cut here
//------------------------------
//------------------------------
// MVC Controller proxy class {proxyFullName} (Generated automatically)
//------------------------------
{ GenerateMvcControllerProxyClass()}
";
    }

    private string GenerateApiInterface(bool withMvc)
    {
        _usings.Clear();
        _sb.Clear();

        _sb.Append(Constants.Namespace).Append(_namespace).AppendLine(Constants.Semicolon).AppendLine()
            .Append(Constants.Public).Append(Constants.Interface).AppendLine(_class)
            .AppendLine(Constants.OpenBlock)
            ;

        foreach (MethodHolder method in _methods)
        {
            List<AttributeHolder> tmp = method.Attributes.ToList();
            ParameterHolder? httpContext = null;
            method.Attributes.Clear();
            if (withMvc)
            {
                httpContext = method.Parameters[0];
                method.Parameters.RemoveAt(0);
            }
            UpdateUsings(method);
            _sb.Append(Constants.Indent).Append(method.ToString()).AppendLine(Constants.Semicolon);
            method.Attributes.AddRange(tmp);
            if(httpContext is { })
            {
                method.Parameters.Insert(0, httpContext);
            }
        }

        _sb.AppendLine(Constants.CloseBlock);

        ApplyUsings();

        return _sb.ToString();
    }

    private string GenerateApiConfiguratorClass()
    {
        _usings.Clear();
        _sb.Clear();

        UpdateUsings(_namespace);
        UpdateUsings(new TypeHolder(typeof(IServiceProvider)));

        TypeHolder baseControllerConfiguratorTh = new TypeHolder(typeof(BaseApiConfigurator));

        UpdateUsings(baseControllerConfiguratorTh);

        _sb.Append(Constants.Namespace).Append(_anotherNamespace).AppendLine(Constants.Semicolon).AppendLine()
            .Append(Constants.Public).Append(Constants.Class).Append(_anotherClass).Append(Constants.Colon).AppendLine(baseControllerConfiguratorTh.TypeName)
            .AppendLine(Constants.OpenBlock).Append(Constants.Indent).Append(Constants.Public).Append(_anotherClass).AppendLine(Constants.Constructor)
            ;

        foreach (MethodHolder method in _methods)
        {
            TypeHolder returnType = new TypeHolder(typeof(Func<>));
            returnType.GenericArguments = method.Parameters.Select(ph => ph.TypeHolder).Concat(new TypeHolder[] { method.ReturnType }).ToArray();
            foreach (TypeHolder th in returnType.GenericArguments)
            {
                UpdateUsings(th);
            }
            UpdateUsings(returnType);
            _sb.Append(Constants.Indent).Append(Constants.Public).Append(returnType).Append(Constants.Space).Append(method.Name).Append(Constants.Arrow);
            foreach (AttributeHolder ah in method.Attributes)
            {
                UpdateUsings(ah);
                _sb.Append(ah).Append(Constants.Space);
            }
            _sb.Append(Constants.Async).Append(Constants.LeftParen);
            int length = _sb.Length;
            foreach (ParameterHolder ph in method.Parameters)
            {
                _sb.Append(ph.Name).Append(Constants.Comma);
            }
            if (length < _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.Append(Constants.RightParen).AppendLine(Constants.Arrow)
                .Append(Constants.Indent).Append(Constants.Indent).Append(Constants.Await).Append(Constants.LeftParen).Append(Constants.LeftParen)
                .Append(method.ReturnType).Append(Constants.RightParen).Append(Constants.CallControllerMethod)
                .Append(Constants.BeginGeneric).Append(_class).Append(Constants.EndGeneric).Append(Constants.LeftParen)
                .Append(Constants.NewObjectArray).Append(Constants.OpenBlock);
            length = _sb.Length;
            foreach (ParameterHolder ph in method.Parameters)
            {
                _sb.Append(ph.Name).Append(Constants.Comma);
            }
            if (length < _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.Append(Constants.CloseBlock).Append(Constants.RightParen).Append(Constants.RightParen).Append(Constants.Dot)
                .AppendLine(Constants.ConfigureAwait).Append(Constants.Indent).Append(Constants.Indent).AppendLine(Constants.Semicolon);
        }

        _sb.AppendLine(Constants.CloseBlock);

        ApplyUsings();

        return _sb.ToString();
    }

    private string GenerateMvcControllerProxyClass()
    {
        _usings.Clear();
        _sb.Clear();

        UpdateUsings(_namespace);

        TypeHolder controllerTh = new TypeHolder(typeof(Controller));
        UpdateUsings(controllerTh);

        TypeHolder baseControllerProxyTh = new TypeHolder(typeof(BaseControllerProxy));
        UpdateUsings(baseControllerProxyTh);

        AttributeHolder connectorAttribute = new(new ConnectorAttribute(_connectorType));

        _sb.Append(Constants.Namespace).Append(_anotherNamespace).AppendLine(Constants.Semicolon).AppendLine()
            .AppendLine(connectorAttribute.ToString())
            .Append(Constants.Public).Append(Constants.Class).Append(_anotherClass).Append(Constants.Colon)
            .Append(baseControllerProxyTh).Append(Constants.Comma).AppendLine(_class)
            .AppendLine(Constants.OpenBlock).Append(Constants.Indent).Append(Constants.Public).Append(_anotherClass).AppendLine(Constants.Constructor)
            ;

        foreach (MethodHolder method in _methods)
        {
            List<AttributeHolder> tmp = method.Attributes.ToList();
            ParameterHolder? httpContext = method.Parameters[0];
            method.Parameters.RemoveAt(0);
            AttributeHolder? authorizeAttribute = method.Attributes.Where(ah => ah.TypeHolder.TypeName == typeof(AuthorizeAttribute).Name).FirstOrDefault();
            if(authorizeAttribute is { })
            {
                UpdateUsings(authorizeAttribute);
                _sb.Append(Constants.Indent).AppendLine(authorizeAttribute.ToString());
            }
            string requestAttributeName = typeof(RoutePathAttribute).Name;

            List<string> allHttpMethods = new();

            foreach (string path in method.Paths)
            {
                AttributeHolder routeAttribute = new(new RouteAttribute(path));
                UpdateUsings(routeAttribute);
                _sb.Append(Constants.Indent).Append(Constants.LeftBraket).Append(routeAttribute.GetNameWithoutAttribute()).Append(Constants.LeftParen)
                    .Append(String.Format(Constants.StringFormat, path))
                    .Append(Constants.RightParen).AppendLine(Constants.RightBraket);
            }
            foreach (string httpMethod in method.HttpMethods)
            {
                AttributeHolder httpMethodAttribute = new(httpMethod switch
                {
                    Constants.Post => new HttpPostAttribute(),
                    Constants.Put => new HttpPutAttribute(),
                    Constants.Delete => new HttpDeleteAttribute(),
                    Constants.Head => new HttpHeadAttribute(),
                    Constants.Patch=> new HttpPatchAttribute(),
                    _ => new HttpGetAttribute()
                });
                UpdateUsings(httpMethodAttribute);

                _sb.Append(Constants.Indent).Append(Constants.LeftBraket).Append(httpMethodAttribute.GetNameWithoutAttribute())
                    .AppendLine(Constants.RightBraket);
            }

            method.Attributes.Clear();
            UpdateUsings(method);

            _sb.Append(Constants.Indent).Append(Constants.Public).Append(Constants.Async).AppendLine(method.ToString()).Append(Constants.Indent)
                .AppendLine(Constants.OpenBlock)
                .Append(Constants.Indent).Append(Constants.Indent).Append(controllerTh.TypeName).Append(Constants.Space)
                .Append(controllerTh.TypeName.ToLower()).Append(Constants.EqualSign)
                .Append(Constants.LeftParen).Append(controllerTh.TypeName).Append(Constants.RightParen).Append(Constants.GetRequiredService)
                .Append(Constants.BeginGeneric).Append(_class).Append(Constants.EndGeneric)
                .Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).Append(Constants.Indent).Append(controllerTh.TypeName.ToLower()).Append(Constants.Dot).Append(Constants.ControllerContext)
                .Append(Constants.EqualSign).Append(Constants.ControllerContext).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).Append(Constants.Indent);
            if(method.ReturnType.GenericArguments is { })
            {
                _sb.Append(Constants.Return);
            }
            _sb.Append(Constants.Await)
                .Append(Constants.LeftParen).Append(Constants.LeftParen).Append(_class)
                .Append(Constants.RightParen).Append(controllerTh.TypeName.ToLower())
                .Append(Constants.RightParen).Append(Constants.Dot)
                .Append(method.Name).Append(Constants.LeftParen);
            int length = _sb.Length;
            foreach(ParameterHolder ph in method.Parameters)
            {
                UpdateUsings(ph);
                _sb.Append(ph.Name).Append(Constants.Comma);
            }
            if(length != _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).AppendLine(Constants.CloseBlock)
                ;
            method.Attributes.AddRange(tmp);
            method.Parameters.Insert(0, httpContext);
        }

        _sb.AppendLine(Constants.CloseBlock);

        ApplyUsings();

        return _sb.ToString();
    }

    private void ApplyUsings()
    {
        if (_usings.Count > 0)
        {
            _sb.Insert(0, Constants.NewLine);
            foreach (string item in _usings)
            {
                _sb.Insert(0, string.Format(Constants.Using, item));
            }
        }
    }

    private void UpdateUsings(MethodHolder methodHolder)
    {
        UpdateUsings(methodHolder.ReturnType);
        foreach (AttributeHolder ah in methodHolder.Attributes)
        {
            UpdateUsings(ah);
        }
        foreach (ParameterHolder ph in methodHolder.Parameters)
        {
            UpdateUsings(ph);
        }
    }

    private void UpdateUsings(ParameterHolder parameterHolder)
    {
        UpdateUsings(parameterHolder.TypeHolder);
        foreach (AttributeHolder ah in parameterHolder.Attributes)
        {
            UpdateUsings(ah);
        }
    }

    private void UpdateUsings(AttributeHolder attributeHolder)
    {
        UpdateUsings(attributeHolder.TypeHolder);
    }

    private bool UpdateUsings(string @namespace)
    {
        int pos = _usings.BinarySearch(@namespace, _namespaceComparer);
        if (pos < 0)
        {
            _usings.Insert(-1 - pos, @namespace);
            return true;
        }
        return false;
    }

    private void UpdateUsings(TypeHolder typeHolder)
    {
        if (UpdateUsings(typeHolder.Namespace))
        {
            if (typeHolder.GenericArguments is { })
            {
                foreach (TypeHolder th in typeHolder.GenericArguments)
                {
                    UpdateUsings(th);
                }
            }
            if (typeHolder.Source is { })
            {
                UpdateUsings(typeHolder.Source);
            }
        }
    }

    private static bool IsNullable(ParameterInfo parameter)
    {
        return parameter.GetCustomAttributes().Any(a => a.GetType().Name.Contains("NullableAttribute"));
    }

    private void CollectRequisites<TConnector>(string fullName, string anotherFullName)
    {
        _connectorType = typeof(TConnector);

        _namespace = fullName.Contains(Constants.Dot) ? fullName.Substring(0, fullName.LastIndexOf(Constants.Dot)) : string.Empty;
        _class = fullName.Contains(Constants.Dot) ? fullName.Substring(fullName.LastIndexOf(Constants.Dot) + 1) : fullName;

        if (!string.IsNullOrEmpty(anotherFullName))
        {
            _anotherNamespace = anotherFullName.Contains(Constants.Dot) ? anotherFullName.Substring(0, anotherFullName.LastIndexOf(Constants.Dot)) : string.Empty;
            _anotherClass = anotherFullName.Contains(Constants.Dot) ? anotherFullName.Substring(anotherFullName.LastIndexOf(Constants.Dot) + 1) : anotherFullName;
        }

        foreach (MethodInfo connectorMethod in _connectorType.GetMethods())
        {
            IEnumerable<RoutePathAttribute> routeAttributes = connectorMethod.GetCustomAttributes<RoutePathAttribute>();
            if (routeAttributes.Count() > 0)
            {
                {
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

                    MethodHolder method = new()
                    {
                        Name = connectorMethod.Name,
                        ReturnType = new TypeHolder(returnType),
                    };

                    if (returnType != connectorMethod.ReturnType)
                    {
                        method.ReturnType.Source = new TypeHolder(connectorMethod.ReturnType);
                    }

                    _methods.Add(method);

                    foreach (RoutePathAttribute routeAttribute in routeAttributes)
                    {
                        method.Paths.Add(routeAttribute.Path);
                    }

                    foreach (HttpMethodAttribute httpMethodAttribute in connectorMethod.GetCustomAttributes<HttpMethodAttribute>())
                    {
                        method.HttpMethods.Add(Utility.GetHttpMethodFromAttribute(httpMethodAttribute));
                    }
                    if(method.HttpMethods.Count == 0)
                    {
                        method.HttpMethods.Add(Constants.Get);
                    }

                    AuthorizationAttribute? attr = connectorMethod.GetCustomAttribute<AuthorizationAttribute>();
                    if (attr is { })
                    {
                        AttributeHolder authorizationAttributeTh = new AttributeHolder(attr);
                        AuthorizeAttribute authorizeAttribute = new AuthorizeAttribute();
                        foreach (PropertyInfo pi in typeof(AuthorizationAttribute).GetProperties())
                        {
                            if (pi.CanWrite)
                            {
                                PropertyInfo? pi1 = typeof(AuthorizeAttribute).GetProperty(pi.Name);
                                if (pi1 is{ } &&  pi1.CanWrite)
                                {
                                    pi1.SetValue(authorizeAttribute, pi.GetValue(attr));
                                }
                            }
                        }
                        AttributeHolder authorizeAttributeTh = new AttributeHolder(authorizeAttribute);
                        authorizeAttributeTh.TypeHolder.Source = authorizationAttributeTh.TypeHolder;

                        method.Attributes.Add(authorizeAttributeTh);
                    }

                    ParameterInfo[] parameters = connectorMethod.GetParameters().Where(p => p.GetCustomAttribute<NotParameterAttribute>() is null).ToArray();

                    method.Parameters.Add(new ParameterHolder { TypeHolder = new TypeHolder(typeof(HttpContext)) });
                    string contextParameterName = Constants.Context;
                    while (parameters.Any(p => p.Name == contextParameterName))
                    {
                        contextParameterName += Constants.Tchar;
                    }
                    method.Parameters.Last().Name = contextParameterName;

                    foreach (ParameterInfo parameter in parameters)
                    {
                        Type parameterType = parameter.ParameterType;
                        if (_dtoServices is { } && _dtoServices.IsRegistered(parameterType))
                        {
                            parameterType = typeof(string);
                        }
                        method.Parameters.Add(new ParameterHolder { TypeHolder = new TypeHolder(parameterType), Name = parameter.Name! });
                        if (parameterType != parameter.ParameterType)
                        {
                            method.Parameters.Last().TypeHolder.Source = new TypeHolder(parameter.ParameterType);
                            method.Parameters.Last().Attributes.Add(new AttributeHolder(new SerializedAttribute(parameter.ParameterType)));
                        }
                        if (IsNullable(parameter))
                        {
                            method.Parameters.Last().TypeHolder.TypeName += Constants.QuestionMark;
                        }
                    }

                }
            }
        }
    }
}
