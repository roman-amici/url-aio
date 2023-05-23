using UrlShortServer.Services;

namespace UrlShortServer.Transport
{
    public class ShortenerResponse
    {
        public ResponseCode Code { get; set; }
        public string Message { get; set; } = null!;
    }
}
