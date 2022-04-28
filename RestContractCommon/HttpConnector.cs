using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Net.Leksi.RestContract;

public class HttpConnector
{
    public event RequestEventHandler? BeforeRequest;
    public event ResponseEventHandler? AfterResponse;

    private HttpClient _httpClient = new();

    public IServiceProvider Services { get; init; }
    public Uri BaseAddress
    {
        get
        {
            return _httpClient.BaseAddress;
        }
        set
        {
            try
            {
                _httpClient.BaseAddress = value;
            }
            catch (InvalidOperationException)
            {
                HttpClient newClient = new HttpClient();
                newClient.BaseAddress = value;
                newClient.Timeout = _httpClient.Timeout;
                foreach(var entry in _httpClient.DefaultRequestHeaders)
                {
                    newClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                }
                _httpClient = newClient;
            }
        }
    }
    public TimeSpan HttpTimeout
    {
        get
        {
            return _httpClient.Timeout;
        }
        set
        {
            if (_httpClient.Timeout != value)
            {
                try
                {
                    _httpClient.Timeout = value;
                }
                catch(InvalidOperationException)
                {
                    HttpClient newClient = new HttpClient();
                    newClient.BaseAddress = _httpClient.BaseAddress;
                    newClient.Timeout = value;
                    foreach (var entry in _httpClient.DefaultRequestHeaders)
                    {
                        newClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                    _httpClient = newClient;
                }
            }
        }
    }

    public HttpRequestHeaders DefaultRequestHeaders
    {
        get
        {
            return _httpClient.DefaultRequestHeaders;
        }
    }
    public HttpConnector(IServiceProvider services)
    {
        Services = services;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, [CallerMemberName] string caller = "")
    {
        BeforeRequest?.Invoke(new RequestEventArgs(request, caller));
        Task<HttpResponseMessage> response = _httpClient.SendAsync(request);
        AfterResponse?.Invoke(new ResponseEventArgs(await response, caller));
        return response.Result;
    }

}
