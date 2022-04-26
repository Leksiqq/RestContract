namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Информационный атрибут, помечает класс прокси контроллера классом коннектора при его генерации из коннектора
/// </para>
/// <para xml:lang="en">
/// Information attribute, marks the controller proxy class when it is generated from the connector
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConnectorAttribute: Attribute
{
    /// <summary>
    /// <para xml:lang="ru">
    /// класс коннектора
    /// </para>
    /// <para xml:lang="en">
    /// connector class
    /// </para>
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализация классом коннектора
    /// </para>
    /// <para xml:lang="en">
    /// Initialization by the connector class
    /// </para>
    /// </summary>
    /// <param name="connectorType"></param>
    public ConnectorAttribute(Type connectorType)
    {
        Type = connectorType;
    }

}
