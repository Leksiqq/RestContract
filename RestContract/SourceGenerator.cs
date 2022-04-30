using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.Dto;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Net.Leksi.RestContract;

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
    private static readonly Regex _routeParts = new Regex(@"^(.*?)(\?.*?)?(#.*?)?$");
    private static readonly Regex _routeParams = new Regex(@"^(?:.*?({\*?([^?:=}\s]+).*?}).*?)*.*$");

    private readonly DtoServiceProvider? _dtoServices;

    private List<MethodHolder> _methods = new();

    private string _controllerNamespace = null!;
    private string _controllerClass = null!;

    private string _proxyNamespace = null!;
    private string _proxyClass = null!;

    private string _connectorNamespace = null!;
    private string _connectorClass = null!;

    private readonly List<string> _usings = new();
    private readonly StringBuilder _sb = new();

    private Type _connectorInterface = null!;

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
    public string GenerateHelpers<TConnector>(string controllerFullName, string proxyFullName, string connectorBaseFullName)
    {
        CollectRequisites<TConnector>(controllerFullName, proxyFullName, connectorBaseFullName);
        return @$"//------------------------------
// MVC Controller interface {controllerFullName} (Generated automatically)
//------------------------------
{GenerateMvcControllerInterface()}
//------------------------------
// Cut here
//------------------------------
//------------------------------
// MVC Controller proxy class {proxyFullName} (Generated automatically)
//------------------------------
{GenerateMvcControllerProxyClass()}
//------------------------------
// Cut here
//------------------------------
//------------------------------
// Connector base class {connectorBaseFullName} (Generated automatically)
//------------------------------
{GenerateConnectorBaseClass()}
";
    }

    private string GenerateConnectorBaseClass()
    {
        _usings.Clear();
        _sb.Clear();

        TypeHolder httpConnectorTh = new TypeHolder(typeof(HttpConnector));
        UpdateUsings(httpConnectorTh);

        TypeHolder StringTh = new TypeHolder(typeof(string));
        UpdateUsings(StringTh);

        UpdateUsings(new TypeHolder(typeof(IServiceProvider)));
        UpdateUsings(Constants.DependencyInjection);

        List<string> usedVariables = new();

        string httpConnectorThArg = httpConnectorTh.TypeName.ToLower();
        string httpConnectorThVariable = GetVariableName($"{Constants.Underscore}{httpConnectorThArg}", usedVariables);

        _sb.Append(Constants.Namespace).Append(_connectorNamespace).AppendLine(Constants.Semicolon).AppendLine()
            .Append(Constants.Public).Append(Constants.Class).AppendLine(_connectorClass)
            .AppendLine(Constants.OpenBlock)
            .Append(Constants.Indent).Append(Constants.Private).Append(Constants.Readonly).Append(httpConnectorTh.TypeName).Append(Constants.Space)
            .Append(httpConnectorThVariable).AppendLine(Constants.Semicolon)
            .Append(Constants.Indent).Append(Constants.Public).Append(_connectorClass).Append(Constants.LeftParen)
            .Append(httpConnectorTh.TypeName).Append(Constants.Space).Append(httpConnectorThArg).AppendLine(Constants.RightParen)
            .Append(Constants.Indent).AppendLine(Constants.OpenBlock)
            .Append(Constants.Indent).Append(Constants.Indent).Append(httpConnectorThVariable)
            .Append(Constants.EqualSign).Append(httpConnectorThArg).AppendLine(Constants.Semicolon)
            .Append(Constants.Indent).AppendLine(Constants.CloseBlock)
            ;

        TypeHolder taskOfResponseTh = new TypeHolder(typeof(Task<HttpResponseMessage>));
        UpdateUsings(taskOfResponseTh);

        TypeHolder optionsTh = new TypeHolder(typeof(JsonSerializerOptions));
        UpdateUsings(optionsTh);

        TypeHolder httpUtilityTh = new TypeHolder(typeof(HttpUtility));
        UpdateUsings(httpUtilityTh);

        TypeHolder converterTh = new TypeHolder(typeof(DtoJsonConverterFactory));

        foreach (MethodHolder method in _methods)
        {
            int usedVariablesCount = usedVariables.Count;
            usedVariables.AddRange(method.Parameters.Select(p => p.Name));
            Dictionary<string, string> parameterAliases = new();

            string optionsThVar = GetVariableName(optionsTh.TypeName.ToLower(), usedVariables);
            string converterThVar = GetVariableName(converterTh.TypeName.ToLower(), usedVariables);

            _sb.Append(Constants.Indent).Append(Constants.Public).Append(taskOfResponseTh)
                .AppendLine(method.ToString(true, false)).Append(Constants.Indent).AppendLine(Constants.OpenBlock);

            Dictionary<string, string> deserialized = new();
            foreach (ParameterHolder parameter in method.Parameters)
            {
                if (
                    (
                        (method.PathMatch?.ContainsKey(parameter.Name.ToLower()) ?? false)
                        || (method.QueryMatch?.ContainsKey(parameter.Name.ToLower()) ?? false)
                    )
                    && parameter.TypeHolder.Source is TypeHolder
                )
                {
                    deserialized.Add(parameter.Name, parameter.TypeHolder.TypeName);
                }
            }

            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(optionsTh).Append(Constants.QuestionMark)
                .Append(Constants.Space).Append(optionsThVar)
                    .Append(Constants.EqualSign).Append(Constants.Null).AppendLine(Constants.Semicolon);


            if (deserialized.Count > 0)
            {

                UpdateUsings(converterTh);



                _sb.Append(Constants.Indent).Append(Constants.Indent).Append(converterTh).Append(Constants.Space).Append(converterThVar)
                    .Append(Constants.EqualSign).Append(httpConnectorThVariable)
                    .Append(Constants.GetRequiredService1).Append(Constants.BeginGeneric).Append(converterTh).Append(Constants.EndGeneric)
                    .Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                    .Append(Constants.Indent).Append(Constants.Indent).Append(optionsThVar)
                    .Append(Constants.EqualSign).Append(Constants.NewObject).Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                    .Append(Constants.Indent).Append(Constants.Indent).Append(optionsThVar).Append(Constants.Dot).Append(Constants.Converters)
                    .Append(Constants.Dot).Append(Constants.Add).Append(Constants.LeftParen).Append(converterThVar).Append(Constants.RightParen)
                    .AppendLine(Constants.Semicolon);

            }

            string path = "";

            for (int i = 0; i < 2; ++i)
            {
                string pathOrQuery = i == 0 ? method.Path : method.Query;
                if(!string.IsNullOrEmpty(pathOrQuery))
                {
                    Dictionary<string, string> match = i == 0 ? method.PathMatch : method.QueryMatch;


                    foreach (ParameterHolder parameter in method.Parameters)
                    {
                        if (match?.ContainsKey(parameter.Name) ?? false)
                        {
                            parameterAliases[parameter.Name] = GetVariableName(parameter.Name, usedVariables);
                            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(StringTh).Append(Constants.Space)
                                .Append(parameterAliases[parameter.Name]).Append(Constants.EqualSign).Append(httpUtilityTh)
                                .Append(Constants.Dot).Append(Constants.UrlEncode).Append(Constants.LeftParen).Append(Constants.Serialize)
                                    .Append(Constants.LeftParen)
                                    .Append(parameter.Name).Append(Constants.Comma).Append(optionsThVar)
                                    .Append(Constants.RightParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon);
                        }
                    }

                    string part = match?.Keys.Aggregate(pathOrQuery, (acc, next) =>
                    {
                        return acc.Replace(match[next], $"{{{parameterAliases[next]}}}");
                    }) ?? pathOrQuery;

                    path += part;
                }
            }

            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(StringTh).Append(Constants.Space).Append(Constants.UrlPath)
                .Append(Constants.EqualSign).Append(Constants.DollarSign).Append(Constants.Quot)
                .Append(path.Replace("\"", "\\\"")).Append(Constants.Quot).AppendLine(Constants.Semicolon);

            TypeHolder httpRequestMessageTh = new TypeHolder(typeof(HttpRequestMessage));
            UpdateUsings(httpRequestMessageTh);
            TypeHolder httpMethodTh = new TypeHolder(typeof(HttpMethod));
            UpdateUsings(httpMethodTh);

            string httpRequestMessageThVar = GetVariableName(httpRequestMessageTh.TypeName.ToLower(), usedVariables);

            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(httpRequestMessageTh).Append(Constants.Space)
                .Append(httpRequestMessageThVar).Append(Constants.EqualSign).Append(Constants.NewObject)
                .Append(Constants.LeftParen).Append(httpMethodTh).Append(Constants.Dot).Append(method.HttpMethod)
                .Append(Constants.Comma).Append(Constants.UrlPath).Append(Constants.RightParen).AppendLine(Constants.Semicolon);

            ContentTypeAttribute? methodContentTypeAttribute = (ContentTypeAttribute?)method.Attributes.Find(a => a.Attribute is ContentTypeAttribute)?.Attribute;
            Dictionary<int, ContentHolder> contents = new();
            foreach (ParameterHolder parameter in method.Parameters)
            {
                if (parameter.Attributes.Find(a => a.Attribute is ContentAttribute)?.Attribute is ContentAttribute contentAttribute)
                {
                    if(methodContentTypeAttribute is null)
                    {
                        throw new Exception($"[{nameof(ContentTypeAttribute).Replace(nameof(Attribute), string.Empty)}(...)] required for method {method.Name}");
                    }

                    ContentHolder holder;
                    if (!contents.ContainsKey(contentAttribute.Part))
                    {
                        holder = new();
                        contents[contentAttribute.Part] = holder;
                    }
                    else
                    {
                        holder = contents[contentAttribute.Part];
                    }
                    bool hasSetAttribute = false;
                    if (parameter.Attributes.Find(a => a.Attribute is SetContentTypeAttribute)?.Attribute is SetContentTypeAttribute setContentTypeAttribute)
                    {
                        if (holder.ContentType is Type || holder.ContentTypeParameter is { })
                        {
                            throw new Exception($"Parameter {parameter.Name} already has {nameof(holder.ContentType)} at method {method.Name}");
                        }
                        if (parameter.TypeHolder.Type != typeof(Type) && parameter.TypeHolder.Source.Type != typeof(Type))
                        {
                            throw new Exception($"Parameter {parameter.Name} must have type {typeof(Type)} at method {method.Name}");
                        }
                        holder.ContentTypeParameter = parameter.Name;
                        hasSetAttribute = true;
                    }
                    if (parameter.Attributes.Find(a => a.Attribute is SetFilenameAttribute)?.Attribute is SetFilenameAttribute setFilenameAttribute)
                    {
                        if (holder.Filename is { } || holder.FilenameParameter is { })
                        {
                            throw new Exception($"Parameter {parameter.Name} already has {nameof(holder.Filename)} at method {method.Name}");
                        }
                        if (parameter.TypeHolder.Type != typeof(string) && parameter.TypeHolder.Source.Type != typeof(string))
                        {
                            throw new Exception($"Parameter {parameter.Name} must have type {typeof(string)} at method {method.Name}");
                        }
                        holder.FilenameParameter = parameter.Name;
                        hasSetAttribute = true;
                    }
                    if (parameter.Attributes.Find(a => a.Attribute is FilenameAttribute)?.Attribute is FilenameAttribute filenameAttribute)
                    {
                        if (holder.Filename is { } || holder.FilenameParameter is { })
                        {
                            throw new Exception($"Parameter {parameter.Name} already has {nameof(holder.Filename)} at method {method.Name}");
                        }
                        if (hasSetAttribute)
                        {
                            throw new Exception($"Parameter {parameter.Name} already has Set... attribute at method {method.Name}");
                        }
                        holder.Filename = filenameAttribute.Filename;
                    }
                    if (parameter.Attributes.Find(a => a.Attribute is ContentTypeAttribute)?.Attribute is ContentTypeAttribute contentTypeAttribute)
                    {
                        if (holder.ContentType is Type || holder.ContentTypeParameter is { })
                        {
                            throw new Exception($"Parameter {parameter.Name} already has {nameof(holder.ContentType)} at method {method.Name}");
                        }
                        if (hasSetAttribute)
                        {
                            throw new Exception($"Parameter {parameter.Name} already has Set... attribute at method {method.Name}");
                        }
                        holder.ContentType = contentTypeAttribute.ContentType;
                    }

                    if(!hasSetAttribute)
                    {
                        if(holder.ValueParameter is { })
                        {
                            throw new Exception($"Parameter {parameter.Name} already has value at method {method.Name}");
                        }
                        holder.ValueParameter = parameter.Name;
                    }

                    if (contents.Count > 1)
                    {
                        if(
                            methodContentTypeAttribute.ContentType != typeof(MultipartFormDataContent)
                            && methodContentTypeAttribute.ContentType != typeof(FormUrlEncodedContent)
                        )
                        {
                            throw new Exception($"method {method.Name}(...) with content type {methodContentTypeAttribute.ContentType} must have only one content part");
                        }
                    }
                }
            }

            foreach(ContentHolder contentHolder in contents.Values)
            {
                if ((contentHolder.Filename is { } || contentHolder.FilenameParameter is { }) 
                    && methodContentTypeAttribute.ContentType != typeof(MultipartFormDataContent))
                {
                    throw new Exception($"Parameter {contentHolder.ValueParameter} have unsupported directive  {nameof(contentHolder.Filename)} at method {method.Name}");
                }

                if (contentHolder.ContentType is null && contentHolder.ContentTypeParameter is null)
                {
                    contentHolder.ContentType = typeof(JsonContent);
                }
            }

            Console.WriteLine(String.Join("\n", contents.Values.Select(v => v.ToString())));

            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(Constants.Return)
                .Append(httpConnectorThVariable).Append(Constants.Dot).Append(Constants.SendAsync).Append(Constants.LeftParen).Append(httpRequestMessageThVar)
                .Append(Constants.RightParen).AppendLine(Constants.Semicolon);

            _sb.Append(Constants.Indent).AppendLine(Constants.CloseBlock);

            usedVariables.RemoveRange(usedVariablesCount, usedVariables.Count - usedVariablesCount);

        }
        _sb.AppendLine(Constants.CloseBlock);

        ApplyUsings();

        return _sb.ToString();
    }

    private static string GetVariableName(string source, List<string> used)
    {
        while (used.Contains(source))
        {
            source = $"{Constants.Underscore}{source}";
        }
        used.Add(source);
        return source;
    }

    private string GenerateMvcControllerInterface()
    {
        _usings.Clear();
        _sb.Clear();

        _sb.Append(Constants.Namespace).Append(_controllerNamespace).AppendLine(Constants.Semicolon).AppendLine()
            .Append(Constants.Public).Append(Constants.Interface).AppendLine(_controllerClass)
            .AppendLine(Constants.OpenBlock)
            ;

        foreach (MethodHolder method in _methods)
        {
            List<AttributeHolder> tmp = method.Attributes.ToList();
            method.Attributes.Clear();
            UpdateUsings(method);

            _sb.Append(Constants.Indent).Append(method.ReturnType.TypeName).Append(Constants.Space)
                .Append(method.Name).Append(Constants.LeftParen);
            int length = _sb.Length;
            foreach (ParameterHolder ph in method.Parameters)
            {
                if (method.PathMatch?.ContainsKey(ph.Name) ?? false)
                {
                    UpdateUsings(ph);
                    _sb.Append(ph.ToString(true, false)).Append(Constants.Comma);
                }
            }
            if (length != _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.Append(Constants.RightParen).AppendLine(Constants.Semicolon);

            method.Attributes.AddRange(tmp);
        }

        _sb.AppendLine(Constants.CloseBlock);

        ApplyUsings();

        return _sb.ToString();
    }

    private string GenerateMvcControllerProxyClass()
    {
        _usings.Clear();
        _sb.Clear();

        UpdateUsings(_controllerNamespace);
        UpdateUsings(Constants.DependencyInjection);

        TypeHolder controllerTh = new TypeHolder(typeof(Controller));
        UpdateUsings(controllerTh);


        _sb.Append(Constants.Namespace).Append(_proxyNamespace).AppendLine(Constants.Semicolon).AppendLine()
            .Append(Constants.Public).Append(Constants.Class).Append(_proxyClass).Append(Constants.Colon)
            .AppendLine(controllerTh.TypeName).AppendLine(Constants.OpenBlock)
            ;

        foreach (MethodHolder method in _methods)
        {
            List<AttributeHolder> tmp = method.Attributes.ToList();
            AttributeHolder? authorizeAttribute = method.Attributes.Where(ah => ah.TypeHolder.TypeName == typeof(AuthorizeAttribute).Name).FirstOrDefault();
            if (authorizeAttribute is { })
            {
                UpdateUsings(authorizeAttribute);
                _sb.Append(Constants.Indent).AppendLine(authorizeAttribute.ToString());
            }
            string requestAttributeName = typeof(RoutePathAttribute).Name;

            List<string> allHttpMethods = new();

            AttributeHolder routeAttribute = new(new RouteAttribute(method.Path));
            UpdateUsings(routeAttribute);
            _sb.Append(Constants.Indent).Append(Constants.LeftBraket).Append(routeAttribute.GetNameWithoutAttribute()).Append(Constants.LeftParen)
                .Append(String.Format(Constants.StringFormat, method.Path))
                .Append(Constants.RightParen).AppendLine(Constants.RightBraket);

            AttributeHolder httpMethodAttribute = new(method.HttpMethod switch
            {
                Constants.Post => new HttpPostAttribute(),
                Constants.Put => new HttpPutAttribute(),
                Constants.Delete => new HttpDeleteAttribute(),
                Constants.Head => new HttpHeadAttribute(),
                Constants.Patch => new HttpPatchAttribute(),
                _ => new HttpGetAttribute()
            });
            UpdateUsings(httpMethodAttribute);

            _sb.Append(Constants.Indent).Append(Constants.LeftBraket).Append(httpMethodAttribute.GetNameWithoutAttribute())
                .AppendLine(Constants.RightBraket);

            method.Attributes.Clear();
            UpdateUsings(method);

            _sb.Append(Constants.Indent).Append(Constants.Public).Append(Constants.Async).Append(method.ReturnType)
                .Append(Constants.Space).Append(method.Name).Append(Constants.LeftParen);
            int length = _sb.Length;
            foreach (ParameterHolder ph in method.Parameters)
            {
                if (method.PathMatch?.ContainsKey(ph.Name) ?? false)
                {
                    UpdateUsings(ph);
                    _sb.Append(ph.ToString(true, false)).Append(Constants.Comma);
                }
            }
            if (length != _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.AppendLine(Constants.RightParen).Append(Constants.Indent)
                .AppendLine(Constants.OpenBlock);
            Dictionary<string, string> deserialized = new();
            foreach (ParameterHolder parameter in method.Parameters)
            {
                if ((method.PathMatch?.ContainsKey(parameter.Name) ?? false) && parameter.TypeHolder.Source is TypeHolder source)
                {
                    deserialized.Add(parameter.Name, source.TypeName);
                }
            }
            if (deserialized.Count > 0)
            {
                TypeHolder converterTh = new TypeHolder(typeof(DtoJsonConverterFactory));
                UpdateUsings(converterTh);
                TypeHolder optionsTh = new TypeHolder(typeof(JsonSerializerOptions));
                UpdateUsings(optionsTh);

                _sb.Append(Constants.Indent).Append(Constants.Indent).Append(converterTh).Append(Constants.Space).Append(converterTh.TypeName.ToLower())
                    .Append(Constants.EqualSign)
                    .Append(Constants.GetRequiredService).Append(Constants.BeginGeneric).Append(converterTh).Append(Constants.EndGeneric)
                    .Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                    .Append(Constants.Indent).Append(Constants.Indent).Append(optionsTh).Append(Constants.Space).Append(optionsTh.TypeName.ToLower())
                    .Append(Constants.EqualSign).Append(Constants.NewObject).Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                    .Append(Constants.Indent).Append(Constants.Indent).Append(optionsTh.TypeName.ToLower()).Append(Constants.Dot).Append(Constants.Converters)
                    .Append(Constants.Dot).Append(Constants.Add).Append(Constants.LeftParen).Append(converterTh.TypeName.ToLower()).Append(Constants.RightParen)
                    .AppendLine(Constants.Semicolon);

                foreach (string key in deserialized.Keys)
                {
                    _sb.Append(Constants.Indent).Append(Constants.Indent).Append(deserialized[key]).Append(Constants.Space)
                        .Append(Constants.Underscore).Append(key).Append(Constants.EqualSign).Append(Constants.Deserialize)
                        .Append(Constants.BeginGeneric).Append(deserialized[key]).Append(Constants.EndGeneric).Append(Constants.LeftParen)
                        .Append(key).Append(Constants.Comma).Append(optionsTh.TypeName.ToLower())
                        .Append(Constants.RightParen).AppendLine(Constants.Semicolon);
                }
            }

            _sb.Append(Constants.Indent).Append(Constants.Indent).Append(controllerTh.TypeName).Append(Constants.Space)
                .Append(controllerTh.TypeName.ToLower()).Append(Constants.EqualSign)
                .Append(Constants.LeftParen).Append(controllerTh.TypeName).Append(Constants.RightParen).Append(Constants.GetRequiredService)
                .Append(Constants.BeginGeneric).Append(_controllerClass).Append(Constants.EndGeneric)
                .Append(Constants.LeftParen).Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).Append(Constants.Indent).Append(controllerTh.TypeName.ToLower()).Append(Constants.Dot).Append(Constants.ControllerContext)
                .Append(Constants.EqualSign).Append(Constants.ControllerContext).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).Append(Constants.Indent);
            if (method.ReturnType.GenericArguments is { })
            {
                _sb.Append(Constants.Return);
            }
            _sb.Append(Constants.Await)
                .Append(Constants.LeftParen).Append(Constants.LeftParen).Append(_controllerClass)
                .Append(Constants.RightParen).Append(controllerTh.TypeName.ToLower())
                .Append(Constants.RightParen).Append(Constants.Dot)
                .Append(method.Name).Append(Constants.LeftParen);
            length = _sb.Length;
            foreach (ParameterHolder ph in method.Parameters)
            {
                if (method.PathMatch?.ContainsKey(ph.Name) ?? false)
                {
                    UpdateUsings(ph);
                    if (deserialized.ContainsKey(ph.Name))
                    {
                        _sb.Append(Constants.Underscore);
                    }
                    _sb.Append(ph.Name).Append(Constants.Comma);
                }
            }
            if (length != _sb.Length)
            {
                _sb.Length -= Constants.Comma.Length;
            }
            _sb.Append(Constants.RightParen).AppendLine(Constants.Semicolon)
                .Append(Constants.Indent).AppendLine(Constants.CloseBlock)
                ;
            method.Attributes.AddRange(tmp);
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

    private void CollectRequisites<TConnector>(string controllerFullName, string proxyFullName, string connectorBaseFullName)
    {
        _connectorInterface = typeof(TConnector);

        _controllerNamespace = controllerFullName.Contains(Constants.Dot) ? controllerFullName.Substring(0, controllerFullName.LastIndexOf(Constants.Dot)) : string.Empty;
        _controllerClass = controllerFullName.Contains(Constants.Dot) ? controllerFullName.Substring(controllerFullName.LastIndexOf(Constants.Dot) + 1) : controllerFullName;

        _proxyNamespace = proxyFullName.Contains(Constants.Dot) ? proxyFullName.Substring(0, proxyFullName.LastIndexOf(Constants.Dot)) : string.Empty;
        _proxyClass = proxyFullName.Contains(Constants.Dot) ? proxyFullName.Substring(proxyFullName.LastIndexOf(Constants.Dot) + 1) : proxyFullName;

        _connectorNamespace = connectorBaseFullName.Contains(Constants.Dot) ? connectorBaseFullName.Substring(0, connectorBaseFullName.LastIndexOf(Constants.Dot)) : string.Empty;
        _connectorClass = connectorBaseFullName.Contains(Constants.Dot) ? connectorBaseFullName.Substring(connectorBaseFullName.LastIndexOf(Constants.Dot) + 1) : connectorBaseFullName;

        foreach (MethodInfo connectorMethod in _connectorInterface.GetMethods())
        {
            if (connectorMethod.GetCustomAttribute<RoutePathAttribute>() is RoutePathAttribute routeAttribute)
            {
                Type returnType = typeof(Task);

                MethodHolder method = new()
                {
                    Name = connectorMethod.Name,
                    ReturnType = new TypeHolder(returnType),
                };

                Match routePartsMatch = _routeParts.Match(routeAttribute.Path);
                if (!routePartsMatch.Success)
                {
                    throw new Exception($"Invalid route format: {routeAttribute.Path}, method: {connectorMethod.Name}(...)");
                }
                for (int iPart = 1; iPart <= 3; iPart++)
                {
                    if (!string.IsNullOrEmpty(routePartsMatch.Groups[iPart].Value))
                    {
                        if (iPart == 3)
                        {
                            throw new Exception($"Hash {routePartsMatch.Groups[iPart].Value} is not allowed, method: {connectorMethod.Name}(...)");
                        }
                        Match routePartMatch = _routeParams.Match(routePartsMatch.Groups[iPart].Value);
                        if (!routePartMatch.Success)
                        {
                            throw new Exception($"Invalid route format: {routeAttribute.Path}, method: {connectorMethod.Name}(...)");
                        }

                        Dictionary<string, string>? matches = null;

                        for (int i = 0; i < routePartMatch.Groups[1].Captures.Count; i++)
                        {
                            if(matches is null)
                            {
                                matches = new();
                            }
                            if (matches.ContainsKey(routePartMatch.Groups[2].Captures[i].Value.ToLower()))
                            {
                                throw new Exception($"Not unique template in route : {routePartMatch.Groups[1].Captures[i].Value}");
                            }
                            ParameterInfo pathParameter = connectorMethod.GetParameters().Where(
                                p => p.Name.ToLower() == routePartMatch.Groups[2].Captures[i].Value.ToLower()).FirstOrDefault();
                            if (pathParameter is null)
                            {
                                throw new Exception($"path parameter is not bound: {routePartMatch.Groups[2].Captures[i].Value}, method: {connectorMethod.Name}(...)");
                            }
                            if (pathParameter.GetCustomAttribute<ContentAttribute>() is { })
                            {
                                throw new Exception($"Both path and body parameter: {routePartMatch.Groups[2].Captures[i].Value}, method: {connectorMethod.Name}(...)");
                            }
                            matches[routePartMatch.Groups[2].Captures[i].Value.ToLower()] = routePartMatch.Groups[1].Captures[i].Value;
                        }
                        switch (iPart)
                        {
                            case 1:
                                method.Path = routePartsMatch.Groups[iPart].Value;
                                method.PathMatch = matches;
                                break;
                            case 2:
                                method.Query = routePartsMatch.Groups[iPart].Value;
                                method.QueryMatch = matches;
                                break;
                        }

                    }
                }

                foreach (CustomAttributeData attribute in connectorMethod.CustomAttributes)
                {
                    method.Attributes.Add(new AttributeHolder(connectorMethod.GetCustomAttribute(attribute.AttributeType)));
                }

                if (returnType != connectorMethod.ReturnType)
                {
                    method.ReturnType.Source = new TypeHolder(connectorMethod.ReturnType);
                }

                _methods.Add(method);

                if (connectorMethod.GetCustomAttribute<HttpMethodAttribute>() is HttpMethodAttribute httpMethodAttribute)
                {
                    method.HttpMethod = GetHttpMethodFromAttribute(httpMethodAttribute);
                }
                else
                {
                    method.HttpMethod = Constants.Get;
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
                            if (pi1 is { } && pi1.CanWrite)
                            {
                                pi1.SetValue(authorizeAttribute, pi.GetValue(attr));
                            }
                        }
                    }
                    AttributeHolder authorizeAttributeTh = new AttributeHolder(authorizeAttribute);
                    authorizeAttributeTh.TypeHolder.Source = authorizationAttributeTh.TypeHolder;

                }

                ParameterInfo[] parameters = connectorMethod.GetParameters()
                    .Where(p => method.PathMatch?.ContainsKey(p.Name.ToLower()) ?? false || p.GetCustomAttribute<ContentAttribute>() is { }).ToArray();

                foreach (ParameterInfo parameter in parameters)
                {
                    Type parameterType = parameter.ParameterType;
                    if (!parameterType.IsPrimitive)
                    {
                        parameterType = typeof(string);
                    }
                    method.Parameters.Add(new ParameterHolder { TypeHolder = new TypeHolder(parameterType), Name = parameter.Name! });
                    foreach(CustomAttributeData attribute in parameter.CustomAttributes)
                    {
                        method.Parameters.Last().Attributes.Add(new AttributeHolder(parameter.GetCustomAttribute(attribute.AttributeType)!));
                    }
                    if (parameterType != parameter.ParameterType)
                    {
                        method.Parameters.Last().TypeHolder.Source = new TypeHolder(parameter.ParameterType);
                    }
                    if (IsNullable(parameter))
                    {
                        method.Parameters.Last().TypeHolder.TypeName += Constants.QuestionMark;
                    }
                }

            }
        }
    }

    private static string GetHttpMethodFromAttribute(HttpMethodAttribute attribute)
    {
        string httpMethod = String.Empty;
        if (attribute.GetType() == typeof(HttpMethodGetAttribute))
        {
            httpMethod = Constants.Get;
        }
        else if (attribute.GetType() == typeof(HttpMethodPostAttribute))
        {
            httpMethod = Constants.Post;
        }
        else if (attribute.GetType() == typeof(HttpMethodPutAttribute))
        {
            httpMethod = Constants.Put;
        }
        else if (attribute.GetType() == typeof(HttpMethodDeleteAttribute))
        {
            httpMethod = Constants.Delete;
        }
        else if (attribute.GetType() == typeof(HttpMethodHeadAttribute))
        {
            httpMethod = Constants.Head;
        }
        else if (attribute.GetType() == typeof(HttpMethodPatchAttribute))
        {
            httpMethod = Constants.Patch;
        }
        return httpMethod;
    }
}
