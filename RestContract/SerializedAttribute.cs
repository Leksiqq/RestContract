namespace Net.Leksi.Server.Contract;

/// <summary>
/// <para xml:lang="ru">
/// Информационный атрибут, помечает параметр метода контроллера, прокси контроллера или API, который имеет в коннекторе 
/// тип, зарегистрированный в DtoServiceProvider, а на стороне сервера преобразованный в строку, в которой ожидается
/// JSON объекта указанного типа
/// </para>
/// <para xml:lang="en">
/// Informational attribute, marks the parameter of the controller method, controller proxy or API that has in the connector
/// a type registered with the DtoServiceProvider and converted on the server side to a string that is expected
/// JSON object of the specified type
/// </para>
/// </summary>

[AttributeUsage(AttributeTargets.Parameter)]
public class SerializedAttribute: Attribute
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Тип исходного параметра в методе коннектора
    /// </para>
    /// <para xml:lang="en">
    /// The type of the initial parameter in the connector method
    /// </para>
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// <para xml:lang="ru">
    /// Инициализация типом исходного параметра в методе коннектора
    /// </para>
    /// <para xml:lang="en">
    /// Initialization with the type of the initial parameter in the connector method
    /// </para>
    /// </summary>
    /// <param name="type"></param>
    public SerializedAttribute(Type type)
    {
        Type = type;
    }
}
