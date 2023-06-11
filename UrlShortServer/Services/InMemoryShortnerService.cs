using System.Collections.Concurrent;
using UrlShortServer.Transport;

namespace UrlShortServer.Services;

public class InMemoryShortenerService : IShortenerService
{
    public InMemoryShortenerService(IShortUrlService service)
    {
        UrlService = service;
    }

    public ConcurrentDictionary<string, string> Cache = new();

    public IShortUrlService UrlService { get; set; }

    public async Task<ShortenerResponse> AddUrl(string longUrl)
    {
        try
        {
            var urlParse = new Uri(longUrl);
        }
        catch
        {
            return new ShortenerResponse()
            {
                Code = ResponseCode.InvalidUrl,
                Message = "Failed to parse long url or url was empty"
            };
        }

        var shortUrl = await UrlService.GenerateShortUrl();

        if (!Cache.TryAdd(shortUrl, longUrl))
        {
            return new ShortenerResponse()
            {
                Code = ResponseCode.ServerError,
                Message = "Failed to add url."
            };
        }

        return new ShortenerResponse()
        {
            Code = ResponseCode.Success,
            Message = shortUrl
        };
    }

    public Task DeleteAllUrls()
    {
        Cache.Clear();

        return Task.CompletedTask;
    }

    public Task<bool> DeleteUrl(string shortUrl)
    {
        return Task.FromResult(Cache.TryRemove(shortUrl, out _));
    }

    public Task<string?> GetLongUrl(string shortUrl)
    {
        Cache.TryGetValue(shortUrl, out var longUrl);

        return Task.FromResult(longUrl);

    }
}