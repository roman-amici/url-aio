using System.Collections.Concurrent;

namespace UrlShortServer.Services;

public interface ICacheService
{
    Task<string?> GetLongUrl(string shortUrl);
    Task AddUrl(string shortUrl, string longUrl);
    Task RemoveUrl(string shortUrl);
    Task RemoveAllUrls();
}

public class NullCacheService : ICacheService
{
    public Task AddUrl(string shortUrl, string longUrl)
    {
        return Task.CompletedTask;
    }

    public Task<string?> GetLongUrl(string shortUrl)
    {
        return Task.FromResult<string?>(null);
    }

    public Task RemoveAllUrls()
    {
        return Task.CompletedTask;
    }

    public Task RemoveUrl(string shortUrl)
    {
        return Task.CompletedTask;
    }
}

public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
    private readonly ConcurrentBag<string> capacity = new ConcurrentBag<string>();
    public int Capacity { get; set; }

    public InMemoryCacheService(int capacity = 10000)
    {
        Capacity = capacity;
    }

    public Task<string?> GetLongUrl(string shortUrl)
    {
        cache.TryGetValue(shortUrl, out var longUrl);

        return Task.FromResult(longUrl);
    }

    public Task AddUrl(string shortUrl, string longUrl)
    {
        if (capacity.Count > Capacity)
        {
            while (capacity.Count > Capacity * 0.9)
            {
                if (capacity.TryTake(out var key))
                {
                    cache.TryRemove(key, out var _);
                }
            }
        }

        if (cache.TryAdd(shortUrl, longUrl))
        {
            capacity.Add(shortUrl);
        }

        return Task.CompletedTask;
    }

    public Task RemoveUrl(string shortUrl)
    {
        cache.TryRemove(shortUrl, out var _);

        return Task.CompletedTask;
    }

    public Task RemoveAllUrls()
    {
        cache.Clear();

        return Task.CompletedTask;
    }
}