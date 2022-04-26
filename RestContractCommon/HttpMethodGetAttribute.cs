namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут, соответствующий HttpGetAttribute из ASP.NET MVC, чтобы не вводить на клиенте лишнюю зависимость.
/// </para>
/// <para xml:lang="en">
/// Attribute corresponding to the HttpGetAttribute from ASP.NET MVC, so as not to introduce an extra dependency on the client.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpMethodGetAttribute: HttpMethodAttribute
{
}
