namespace Net.Leksi.RestContract;

internal class Client
{
    internal void Run(Uri baseAddress, string secretWord, Dictionary<string, string> target)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = baseAddress;
        client.DefaultRequestHeaders.Add(Server.SecretWordHeader, secretWord);
        foreach(string key in target.Keys)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, key);

            HttpResponseMessage response = client.Send(request);

            if (response.IsSuccessStatusCode)
            {
                target[key] = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                target[key] = "// Not implemented yet";
            }
        }
    }
}
