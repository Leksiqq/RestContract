using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.Leksi.DocsRazorator;
using Net.Leksi.Dto;
using Net.Leksi.RestContract.Pages;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Net.Leksi.RestContract;

public class HelpersBuilder : IControllerInterfaceBuilder, IControllerProxyBuilder, IConnectorBaseBuilder
{

    private static readonly Regex _routeParts = new Regex(@"^(.*?)(\?.*?)?(#.*?)?$");
    private static readonly Regex _routeParams = new Regex(@"^(?:.*?({\*?([^?:=}\s]+).*?}).*?)*.*$");
    private static readonly Regex _fullName = new Regex(@"^(?:(.+)\.)?([^.]+)$");

    private readonly DtoServiceProvider? _dtoServices;

    private List<MethodHolder> _methods = new();

    private string _controllerNamespace = null!;
    private string _controllerClass = null!;

    private string _proxyNamespace = null!;
    private string _proxyClass = null!;

    private string _connectorNamespace = null!;
    private string _connectorClass = null!;

    private readonly List<string> _usings = new();

    private Type _connectorInterface = null!;

    private readonly NamespaceComparer _namespaceComparer = new();

    public HelpersBuilder(IServiceProvider services)
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
    public async Task BuildHelpers<TConnector>(string controllerInterfaceFullName, string controllerProxyFullName,
        string connectorBaseFullName)
    {
        CollectRequisites<TConnector>(controllerInterfaceFullName, controllerProxyFullName, connectorBaseFullName);
        Generator generator = new();
        await foreach (KeyValuePair<string, object> result in generator.Generate(
            new object[] {
                new KeyValuePair<Type, object>(typeof(IConnectorBaseBuilder), this),
                new KeyValuePair<Type, object>(typeof(IControllerInterfaceBuilder), this),
                new KeyValuePair<Type, object>(typeof(IControllerProxyBuilder), this)
            },
            new string[] {
                "ConnectorBase",
                "ControllerInterface",
                "ControllerProxy",
            }
        ))
        {
            if (result.Value is Exception)
            {
                Console.WriteLine($"// {result.Key}");
            }
            Console.WriteLine(result.Value);
        }

    }

    public void BuildConnectorBase(ConnectorBaseModel model)
    {
        _usings.Clear();
        List<string> usedVariables = new();
        usedVariables.Add("_httpConnector");

        model.NamespaceValue = _connectorNamespace;
        model.ClassName = _connectorClass;

        model.Methods = new List<MethodModel>();

        UpdateUsings(new TypeHolder(typeof(HttpConnector)));
        UpdateUsings(new TypeHolder(typeof(HttpResponseMessage)));


        foreach (MethodHolder method in _methods)
        {
            int usedVariablesCount = usedVariables.Count;

            UpdateUsings(method);
            MethodModel methodModel = new()
            {
                Name = method.Name,
                Type = method.ReturnType.TypeName,
                Parameters = new List<ParameterModel>(),
            };
            foreach (ParameterHolder ph in method.Parameters)
            {
                int caseMatch = 0;
                if (((method.PathMatch?.ContainsKey(ph.Name) ?? false) && (caseMatch = 1) == caseMatch)
                    || (ph.Attributes.Find(a => a.Attribute is BodyAttribute) is { } && (caseMatch = 2) == caseMatch)
                )
                {
                    usedVariables.Add(ph.Name);
                    UpdateUsings(ph);
                    ParameterModel pm = new() { Name = ph.Name, Type = ph.TypeHolder.Source?.TypeName ?? ph.TypeHolder.TypeName };
                    methodModel.Parameters.Add(pm);
                    if(caseMatch == 1 && ph.TypeHolder.Source is { })
                    {
                        methodModel.HasSerialized = true;
                        if (methodModel.Deserializing is null)
                        {
                            TypeHolder converterTh = new TypeHolder(typeof(DtoJsonConverterFactory));
                            UpdateUsings(converterTh);
                            TypeHolder optionsTh = new TypeHolder(typeof(JsonSerializerOptions));
                            UpdateUsings(optionsTh);
                            TypeHolder httpUtilityTh = new TypeHolder(typeof(HttpUtility));
                            UpdateUsings(httpUtilityTh);
                            TypeHolder keyProcessingTh = new TypeHolder(typeof(KeysProcessing));
                            UpdateUsings(keyProcessingTh);

                            methodModel.OptionsVariable = GetVariableName("getOptions", usedVariables);
                            methodModel.ConverterVariable = GetVariableName("getConverter", usedVariables);
                            methodModel.Deserializing = new();
                        }
                        string localVariable = GetVariableName(ph.Name, usedVariables);
                        methodModel.Deserializing.Add(new Tuple<string, string, string>(ph.TypeHolder.Source.TypeName,
                            localVariable, ph.Name));
                    }
                }
            }
            model.Methods.Add(methodModel);
            usedVariables.RemoveRange(usedVariablesCount, usedVariables.Count - usedVariablesCount);
        }
        model.Usings = _usings;
    }

    public void BuildControllerProxy(ControllerProxyModel model)
    {
        _usings.Clear();
        List<string> usedVariables = new();

        model.NamespaceValue = _proxyNamespace;
        model.ClassName = _proxyClass;

        model.Methods = new List<MethodModel>();

        UpdateUsings(new TypeHolder(typeof(Controller)));


        foreach (MethodHolder method in _methods)
        {
            usedVariables.Clear();

            UpdateUsings(method);
            MethodModel methodModel = new()
            {
                Name = method.Name,
                Type = method.ReturnType.TypeName,
                Parameters = new List<ParameterModel>(),
                Attributes = new List<AttributeModel>()
            };

            AttributeHolder? routePathAH = method.Attributes.Where(ah => ah.TypeHolder.TypeName == typeof(RoutePathAttribute).Name).FirstOrDefault();
            if (routePathAH is { } && routePathAH.Attribute is RoutePathAttribute routePathAttribute)
            {
                AttributeHolder routeAH = new AttributeHolder(new RouteAttribute(routePathAttribute.Path));
                UpdateUsings(routeAH);
                AttributeModel am = new() { Name = routeAH.GetNameWithoutAttribute() };
                am.Properties[String.Empty] = $@"""{routePathAttribute.Path}""";
                methodModel.Attributes.Add(am);
            }

            AttributeHolder httpMethodAttribute = new(method.HttpMethod switch
            {
                "Post" => new HttpPostAttribute(),
                "Put" => new HttpPutAttribute(),
                "Delete" => new HttpDeleteAttribute(),
                "Head" => new HttpHeadAttribute(),
                "Patch" => new HttpPatchAttribute(),
                _ => new HttpGetAttribute()
            });
            UpdateUsings(httpMethodAttribute);
            AttributeModel httpMethodAttributeModel = new() { Name = httpMethodAttribute.GetNameWithoutAttribute() };
            methodModel.Attributes.Add(httpMethodAttributeModel);

            AttributeHolder? authorizationAH = method.Attributes.Where(ah => ah.TypeHolder.TypeName == typeof(AuthorizationAttribute).Name).FirstOrDefault();
            if (authorizationAH is { } && authorizationAH.Attribute is AuthorizationAttribute authorizationAttribute)
            {
                AttributeHolder authorizeAH = new AttributeHolder(new AuthorizeAttribute());
                UpdateUsings(authorizeAH);
                AttributeModel am = new() { Name = authorizeAH.GetNameWithoutAttribute() };
                if (!string.IsNullOrEmpty(authorizationAttribute.AuthenticationSchemes))
                {
                    am.Properties[nameof(authorizationAttribute.AuthenticationSchemes)] = $@"""{authorizationAttribute.AuthenticationSchemes}""";
                }
                if (!string.IsNullOrEmpty(authorizationAttribute.Roles))
                {
                    am.Properties[nameof(authorizationAttribute.Roles)] = $@"""{authorizationAttribute.Roles}""";
                }
                if (!string.IsNullOrEmpty(authorizationAttribute.Policy))
                {
                    am.Properties[nameof(authorizationAttribute.Policy)] = $@"""{authorizationAttribute.Policy}""";
                }
                methodModel.Attributes.Add(am);
            }

            foreach (ParameterHolder ph in method.Parameters)
            {
                int caseMatch = 0;
                if (((method.PathMatch?.ContainsKey(ph.Name) ?? false) && (caseMatch = 1) == caseMatch)
                    || (ph.Attributes.Find(a => a.Attribute is BodyAttribute) is { } && (caseMatch = 2) == caseMatch)
                )
                {
                    if (caseMatch == 1)
                    {
                        usedVariables.Add(ph.Name);
                    }
                    if (caseMatch == 1 && ph.TypeHolder.Source is { } || caseMatch == 2)
                    {
                        methodModel.HasSerialized = true;
                        if (methodModel.Deserializing is null)
                        {
                            TypeHolder converterTh = new TypeHolder(typeof(DtoJsonConverterFactory));
                            UpdateUsings(converterTh);
                            TypeHolder optionsTh = new TypeHolder(typeof(JsonSerializerOptions));
                            UpdateUsings(optionsTh);

                            methodModel.OptionsVariable = GetVariableName("options", usedVariables);
                            methodModel.ConverterVariable = GetVariableName("converter", usedVariables);
                            methodModel.Deserializing = new();
                        }
                        string localVariable = GetVariableName(ph.Name, usedVariables);
                        methodModel.Deserializing.Add(new Tuple<string, string, string>(ph.TypeHolder.Source.TypeName,
                            localVariable, caseMatch == 1 ? ph.Name : null));
                        methodModel.ControllerParameters.Add(localVariable);
                    }
                    else
                    {
                        methodModel.ControllerParameters.Add(ph.Name);
                    }
                    UpdateUsings(ph);
                    if (caseMatch == 1)
                    {
                        ParameterModel pm = new() { Name = ph.Name, Type = ph.TypeHolder.TypeName };
                        methodModel.Parameters.Add(pm);
                    }
                }
            }
            UpdateUsings(_controllerNamespace);
            methodModel.ControllerInterfaceClassName = _controllerClass;
            methodModel.ControllerVariable = GetVariableName("controller", usedVariables);
            model.Methods.Add(methodModel);
        }
        model.Usings = _usings.Where(ns => ns != _proxyNamespace).ToList();
    }
    public void BuildControllerInterface(ControllerInterfaceModel model)
    {
        _usings.Clear();

        model.NamespaceValue = _controllerNamespace;
        model.ClassName = _controllerClass;

        model.Methods = new List<MethodModel>();

        foreach (MethodHolder method in _methods)
        {
            UpdateUsings(method);
            MethodModel methodModel = new() { Name = method.Name, Type = method.ReturnType.TypeName, Parameters = new List<ParameterModel>() };
            foreach (ParameterHolder ph in method.Parameters)
            {
                if (method.PathMatch?.ContainsKey(ph.Name) ?? false || ph.Attributes.Find(a => a.Attribute is BodyAttribute) is { })
                {
                    UpdateUsings(ph);
                    ParameterModel parameterModel = new() { Name = ph.Name, Type = (ph.TypeHolder.Source?.TypeName ?? ph.TypeHolder.TypeName) };
                    methodModel.Parameters.Add(parameterModel);
                }
            }
            model.Methods.Add(methodModel);
        }


        model.Usings = _usings;
    }
    private void CollectRequisites<TConnector>(string controllerFullName, string proxyFullName, string connectorBaseFullName)
    {
        _connectorInterface = typeof(TConnector);

        Match fullNameMatch = _fullName.Match(controllerFullName);
        if (fullNameMatch.Success)
        {
            _controllerNamespace = !string.IsNullOrEmpty(fullNameMatch.Groups[1].Value) ? fullNameMatch.Groups[1].Value : String.Empty;
            _controllerClass = fullNameMatch.Groups[2].Value;
        }

        fullNameMatch = _fullName.Match(proxyFullName);
        if (fullNameMatch.Success)
        {
            _proxyNamespace = !string.IsNullOrEmpty(fullNameMatch.Groups[1].Value) ? fullNameMatch.Groups[1].Value : String.Empty;
            _proxyClass = fullNameMatch.Groups[2].Value;
        }

        fullNameMatch = _fullName.Match(connectorBaseFullName);
        if (fullNameMatch.Success)
        {
            _connectorNamespace = !string.IsNullOrEmpty(fullNameMatch.Groups[1].Value) ? fullNameMatch.Groups[1].Value : String.Empty;
            _connectorClass = fullNameMatch.Groups[2].Value;
        }

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
                            if (matches is null)
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
                            if (pathParameter.GetCustomAttribute<BodyAttribute>() is { })
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
                    Attribute att = connectorMethod.GetCustomAttribute(attribute.AttributeType);
                    AttributeHolder? ah = null;
                    if (att is HttpMethodAttribute httpMethodAttribute)
                    {
                        method.HttpMethod = GetHttpMethodFromAttribute(httpMethodAttribute);
                    }
                    else
                    {
                        ah = new AttributeHolder(connectorMethod.GetCustomAttribute(attribute.AttributeType));
                    }
                    if (ah is { })
                    {
                        method.Attributes.Add(ah);
                    }
                }

                if (returnType != connectorMethod.ReturnType)
                {
                    method.ReturnType.Source = new TypeHolder(connectorMethod.ReturnType);
                }

                _methods.Add(method);

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
                    .Where(p => method.PathMatch?.ContainsKey(p.Name.ToLower()) ?? false || p.GetCustomAttribute<BodyAttribute>() is { }).ToArray();

                if (parameters.Where(p => p.GetCustomAttribute<BodyAttribute>() is { }).Count() > 1)
                {
                    throw new Exception($"Second body parameter: {parameters.Where(p => p.GetCustomAttribute<BodyAttribute>() is { }).Last().Name}, method: {connectorMethod.Name}(...)");
                }

                foreach (ParameterInfo parameter in parameters)
                {
                    Type parameterType = parameter.ParameterType;
                    if (!parameterType.IsPrimitive)
                    {
                        parameterType = typeof(string);
                    }
                    method.Parameters.Add(new ParameterHolder { TypeHolder = new TypeHolder(parameterType), Name = parameter.Name! });
                    foreach (CustomAttributeData attribute in parameter.CustomAttributes)
                    {
                        method.Parameters.Last().Attributes.Add(new AttributeHolder(parameter.GetCustomAttribute(attribute.AttributeType)!));
                    }
                    if (parameterType != parameter.ParameterType)
                    {
                        method.Parameters.Last().TypeHolder.Source = new TypeHolder(parameter.ParameterType);
                    }
                    if (IsNullable(parameter))
                    {
                        method.Parameters.Last().TypeHolder.TypeName += "?";
                    }
                }

            }
        }
    }

    private static bool IsNullable(ParameterInfo parameter)
    {
        return parameter.GetCustomAttributes().Any(a => a.GetType().Name.Contains("NullableAttribute"));
    }

    private static string GetHttpMethodFromAttribute(HttpMethodAttribute attribute)
    {
        string httpMethod = String.Empty;
        if (attribute.GetType() == typeof(HttpMethodGetAttribute))
        {
            httpMethod = "Get";
        }
        else if (attribute.GetType() == typeof(HttpMethodPostAttribute))
        {
            httpMethod = "Post";
        }
        else if (attribute.GetType() == typeof(HttpMethodPutAttribute))
        {
            httpMethod = "Put";
        }
        else if (attribute.GetType() == typeof(HttpMethodDeleteAttribute))
        {
            httpMethod = "Delete";
        }
        else if (attribute.GetType() == typeof(HttpMethodHeadAttribute))
        {
            httpMethod = "Head";
        }
        else if (attribute.GetType() == typeof(HttpMethodPatchAttribute))
        {
            httpMethod = "Patch";
        }
        return httpMethod;
    }

    private void UpdateUsings(MethodHolder methodHolder)
    {
        UpdateUsings(methodHolder.ReturnType);
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

    private static string GetVariableName(string source, List<string> used)
    {
        while (used.Contains(source))
        {
            source = $"_{source}";
        }
        used.Add(source);
        return source;
    }

}
