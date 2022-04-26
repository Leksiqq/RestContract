namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут помечающий параметр метода коннектора, значение которого будет передано в теле HTTP-запроса.
/// </para>
/// <para xml:lang="en">
/// An attribute that marks the parameter of the connector method as not present in the controller method.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class BodyAttribute: Attribute
{
}
