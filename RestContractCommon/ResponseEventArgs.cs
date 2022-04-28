namespace Net.Leksi.RestContract;

public class ResponseEventArgs: EventArgs
{
    public HttpResponseMessage Response { get; init; }
    public string Caller { get; init; }

    public ResponseEventArgs(HttpResponseMessage response, string caller)
    {
        Response = response;
        Caller = caller;
    }
}
