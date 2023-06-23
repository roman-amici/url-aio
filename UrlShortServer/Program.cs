using Microsoft.EntityFrameworkCore;
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
