using System.Net.Http.Json;

namespace Net.Leksi.RestContract;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public class ContentTypeAttribute: Attribute
{
    public Type ContentType { get; init; }

    public ContentTypeAttribute(Type contentType)
    {
        if (!typeof(HttpContent).IsAssignableFrom(contentType))
        {
            throw new Exception($"{nameof(contentType)} must be {nameof(HttpContent)}");
        }
        if(
            contentType != typeof(MultipartFormDataContent)
            && contentType != typeof(FormUrlEncodedContent)
            && contentType != typeof(JsonContent)
            && contentType != typeof(ByteArrayContent)
            && contentType != typeof(StreamContent)
            && contentType != typeof(StringContent)
        )
        {
            throw new Exception($"{contentType} is unsupported");
        }
        ContentType = contentType;
    }
}
