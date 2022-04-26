namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут, соответствующий RouteAttribute из ASP.NET MVC, чтобы не вводить на клиенте лишнюю зависимость.
/// </para>
/// <para xml:lang="en">
/// Attribute corresponding to the RouteAttribute from ASP.NET MVC, so as not to introduce an extra dependency on the client.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RoutePathAttribute : Attribute
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, соответствующее свойству Template класса RouteAttribute
    /// </para>
    /// <para xml:lang="en">
    /// Property corresponding to the Template property of the RouteAttribute class
    /// </para>
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализация 
    /// </para>
    /// <para xml:lang="ru">
    /// Initialization 
    /// </para>
    /// </summary>
    /// <param name="path"></param>
    public RoutePathAttribute(string path)
    {
        Path = path;
    }
}
