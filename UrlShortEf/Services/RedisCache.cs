using StackExchange.Redis;

namespace UrlShortServer.Services;

public class RedisCache : ICacheService
{
    IDatabase Db { get; set; }
    IServer Server { get; set; }

    public RedisCache(IDatabase db, IServer server)
    {
        Db = db;
        Server = server;
    }

    public async Task AddUrl(string shortUrl, string longUrl)
    {
        await Db.StringSetAsync(shortUrl, longUrl);
    }

    public async Task<string?> GetLongUrl(string shortUrl)
    {
        return await Db.StringGetAsync(shortUrl);
    }

    public Task RemoveAllUrls()
    {
        return Server.FlushAllDatabasesAsync();
    }

    public Task RemoveUrl(string shortUrl)
    {
        return Db.KeyDeleteAsync(shortUrl);
    }
}