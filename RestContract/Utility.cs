namespace Net.Leksi.RestContract;

internal static class Utility
{
    internal static string GetHttpMethodFromAttribute(HttpMethodAttribute attribute)
    {
        string httpMethod = String.Empty;
        if (attribute.GetType() == typeof(HttpMethodGetAttribute))
        {
            httpMethod = Constants.Get;
        }
        else if (attribute.GetType() == typeof(HttpMethodPostAttribute))
        {
            httpMethod = Constants.Post;
        }
        else if (attribute.GetType() == typeof(HttpMethodPutAttribute))
        {
            httpMethod = Constants.Put;
        }
        else if (attribute.GetType() == typeof(HttpMethodDeleteAttribute))
        {
            httpMethod = Constants.Delete;
        }
        else if (attribute.GetType() == typeof(HttpMethodHeadAttribute))
        {
            httpMethod = Constants.Head;
        }
        else if (attribute.GetType() == typeof(HttpMethodPatchAttribute))
        {
            httpMethod = Constants.Patch;
        }
        return httpMethod;
    }

}
