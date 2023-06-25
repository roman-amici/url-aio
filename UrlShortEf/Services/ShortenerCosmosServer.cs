using Microsoft.Azure.Cosmos;
using UrlShortServer.Database;
using UrlShortServer.Transport;

namespace UrlShortServer.Services;

public class ShortenerCosmosService : IShortenerService
{
    CosmosClient client;
    ICacheService cache;
    IShortUrlService shortUrlGenerator;

    public const string dbName = "shortUrl";
    public const string containerName = "shortUrl";

    public ShortenerCosmosService(
        CosmosClient client,
        ICacheService cache,
        IShortUrlService shortGenerator)
    {
        this.client = client;
        this.cache = cache;
        shortUrlGenerator = shortGenerator;
    }

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

        var shortUrl = await shortUrlGenerator.GenerateShortUrl();

        var entry = new CosmosUrlEntry()
        {
            id = shortUrl,
            longUrl = longUrl
        };

        var container = client.GetContainer(dbName, containerName);
        await container.UpsertItemAsync(entry);

        await cache.AddUrl(shortUrl, longUrl);

        return new ShortenerResponse()
        {
            Code = ResponseCode.Success,
            Message = shortUrl
        };
    }

    public async Task DeleteAllUrls()
    {
        var container = client.GetContainer(dbName, containerName);
        await container.DeleteContainerAsync();
        var db = client.GetDatabase(dbName);
        await db.CreateContainerIfNotExistsAsync(new ContainerProperties(containerName, "/id"));

        await cache.RemoveAllUrls();
    }

    public async Task<bool> DeleteUrl(string shortUrl)
    {
        var container = client.GetContainer(dbName, containerName);

        var entry = await container
            .ReadItemAsync<CosmosUrlEntry>(shortUrl, new PartitionKey(shortUrl));

        if (entry == null)
        {
            return false;
        }

        await container.DeleteItemAsync<UrlEntry>(
            shortUrl,
            new PartitionKey(shortUrl));

        await cache.RemoveUrl(shortUrl);

        return true;
    }

    public async Task EnsureCreated()
    {
        await client.CreateDatabaseIfNotExistsAsync(dbName);
        var database = client.GetDatabase(dbName);
        await database.CreateContainerIfNotExistsAsync(new ContainerProperties(containerName, "/id"));
    }

    public async Task<string?> GetLongUrl(string shortUrl)
    {
        var container = client.GetContainer(dbName, containerName);

        var cacheLongUrl = await cache.GetLongUrl(shortUrl);

        if (!string.IsNullOrEmpty(cacheLongUrl))
        {
            return cacheLongUrl;
        }

        var entry = await container.ReadItemAsync<CosmosUrlEntry>(
            shortUrl, new PartitionKey(shortUrl));

        var entryLongUrl = entry?.Resource?.longUrl;

        if (string.IsNullOrEmpty(cacheLongUrl) && !string.IsNullOrEmpty(entryLongUrl))
        {
            _ = Task.Run(() => cache.AddUrl(shortUrl, entryLongUrl));
        }

        return entryLongUrl;
    }
}