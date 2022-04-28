namespace Net.Leksi.RestContract;

public class RequestEventArgs: EventArgs
{
    public HttpRequestMessage Request { get; init; }
    public string Caller { get; init; }

    public RequestEventArgs(HttpRequestMessage request, string caller)
    {
        Request = request;
        Caller = caller;
    }
}
