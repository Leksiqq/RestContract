namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут, соответствующий AuthorizeAttribute из ASP.NET, чтобы не вводить на клиенте лишнюю зависимость.
/// </para>
/// <para xml:lang="en">
/// An attribute that matches the AuthorizeAttribute from ASP.NET so as not to introduce an extra dependency on the client.
/// </para>/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuthorizationAttribute : Attribute
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Соответствует свойству из AuthorizeAttribute
    /// </para>
    /// <para xml:lang="en">
    /// Corresponds to the property from AuthorizeAttribute
    /// </para>
    /// </summary>
    public string? Policy { get; set; }
    /// <summary>
    /// <para xml:lang="ru">
    /// Соответствует свойству из AuthorizeAttribute
    /// </para>
    /// <para xml:lang="en">
    /// Corresponds to the property from AuthorizeAttribute
    /// </para>
    /// </summary>
    public string? Roles { get; set; }
    /// <summary>
    /// <para xml:lang="ru">
    /// Соответствует свойству из AuthorizeAttribute
    /// </para>
    /// <para xml:lang="en">
    /// Corresponds to the property from AuthorizeAttribute
    /// </para>
    /// </summary>
    public string? AuthenticationSchemes { get; set; }
}
