namespace StarStream
{
    using System.Net.Http;

    class ClientBuilder
    {
        public static HttpClient BuildKrakenClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-Id", Program.Configuration.TwitchClientId);
            return client;
        }
    }
}
