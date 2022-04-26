namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут, соответствующий HttpPutAttribute из ASP.NET MVC, чтобы не вводить на клиенте лишнюю зависимость.
/// </para>
/// <para xml:lang="en">
/// Attribute corresponding to the HttpPutAttribute from ASP.NET MVC, so as not to introduce an extra dependency on the client.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpMethodPutAttribute: HttpMethodAttribute
{
}
