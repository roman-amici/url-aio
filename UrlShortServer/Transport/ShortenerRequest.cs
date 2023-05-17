namespace UrlShortServer.Transport
{
    public class ShortenerRequest
    {
        public string BaseUrl { get; set; } = null!;
        public double MaxCacheTimeSeconds { get; set; } = 60 * 60;
    }
}
