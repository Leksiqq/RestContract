namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут, соответствующий HttpHeadAttribute из ASP.NET MVC, чтобы не вводить на клиенте лишнюю зависимость.
/// </para>
/// <para xml:lang="en">
/// Attribute corresponding to the HttpHeadAttribute from ASP.NET MVC, so as not to introduce an extra dependency on the client.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpMethodHeadAttribute: HttpMethodAttribute
{
}
