namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Родительский класс для атрибутов, привязывающих http-методы к методам "коннектора"
/// </para>
/// <para xml:lang="en">
/// Parent class for attributes binding http methods to "connector" methods
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpMethodAttribute: Attribute
{
}
