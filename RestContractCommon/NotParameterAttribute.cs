namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Атрибут помечающий параметр метода коннектора, как отсутствующий в методе контроллера.
/// </para>
/// <para xml:lang="en">
/// An attribute that marks the parameter of the connector method as not present in the controller method.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class NotParameterAttribute: Attribute
{
}
