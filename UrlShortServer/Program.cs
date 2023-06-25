using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortServer.Database;
using UrlShortServer.Services;

var builder = WebApplication.CreateBuilder(args);

var dbType = builder.Configuration.GetValue<string>("DbType")?.ToLowerInvariant();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (dbType == "inmemory")
{
    builder.Services.AddSingleton<IShortenerService, InMemoryShortenerService>();
}
else if (dbType == "cosmos")
{
    var connectionString = builder.Configuration.GetValue<string>("DbConnectionString");
    var cosmosClient = new CosmosClient(connectionString);
    builder.Services.AddSingleton<CosmosClient>(cosmosClient);

    builder.Services.AddScoped<IShortenerService, ShortenerCosmosService>();
}
else
{
    builder.Services.AddDbContext<UrlDbContext>(options =>
    {

        var connectionString = builder.Configuration.GetValue<string>("DbConnectionString");

        if (dbType == "postgres")
        {
            options.UseNpgsql(connectionString);
        }
        else if (dbType == "sqlite")
        {
            options.UseSqlite(connectionString);
        }
    });

    builder.Services.AddScoped<IShortenerService, ShortenerService>();
}

var cacheType = builder.Configuration.GetValue<string>("CacheType")?.ToLowerInvariant();

if (cacheType == "inmemory")
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}
else if (cacheType == "redis")
{
    var cacheConnectionString = builder.Configuration.GetValue<string>("CacheConnectionString");
    var mp = ConnectionMultiplexer.Connect(cacheConnectionString);
    var endpoint = cacheConnectionString.Split(',').First();

    builder.Services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(x => mp);
    builder.Services.AddScoped<IServer>(x =>
    {
        var redis = x.GetService<IConnectionMultiplexer>();
        if (redis == null)
        {
            throw new InvalidOperationException("Failed to get redis server");
        }

        return redis.GetServer(endpoint, 6379);
    });
    builder.Services.AddScoped<IDatabase>(x =>
    {
        var redis = x.GetService<IConnectionMultiplexer>();
        if (redis == null)
        {
            throw new InvalidOperationException("Failed to get redis server");
        }

        return redis.GetDatabase();
    });
    builder.Services.AddScoped<ICacheService, RedisCache>();
}
else
{
    builder.Services.AddSingleton<ICacheService, NullCacheService>();
}

builder.Services.AddSingleton<IShortUrlService, RandomShortUrlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// app.UseSwagger();
// app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
