using Microsoft.EntityFrameworkCore;
using UrlShortServer.Database;
using UrlShortServer.Transport;

namespace UrlShortServer.Services
{
    public interface IShortenerService
    {
        Task<string?> GetLongUrl(string shortUrl);

        Task<ShortenerResponse> AddUrl(string longUrl);

        Task DeleteAllUrls();

        Task<bool> DeleteUrl(string shortUrl);

        Task EnsureCreated();
    }

    public class ShortenerService : IShortenerService
    {
        IShortUrlService shortUrlGenerator;
        UrlDbContext db;
        ICacheService cache;

        public ShortenerService(IShortUrlService shortGenerator, UrlDbContext db, ICacheService cache)
        {
            shortUrlGenerator = shortGenerator;
            this.db = db;
            this.cache = cache;
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

            var entry = new UrlEntry()
            {
                ShortUrl = shortUrl,
                LongUrl = longUrl
            };

            db.UrlEntries.Add(entry);

            await db.SaveChangesAsync();

            await cache.AddUrl(shortUrl, longUrl);

            return new ShortenerResponse()
            {
                Code = ResponseCode.Success,
                Message = shortUrl
            };
        }

        public async Task<bool> DeleteUrl(string shortUrl)
        {
            var entry = await db.UrlEntries.FirstOrDefaultAsync(x => x.ShortUrl == shortUrl);

            if (entry == null)
            {
                return false;
            }

            db.UrlEntries.Remove(entry);

            await db.SaveChangesAsync();

            await cache.RemoveUrl(shortUrl);

            return true;
        }

        public async Task DeleteAllUrls()
        {
            if (db.Database.IsNpgsql())
            {
                await db.Database.ExecuteSqlRawAsync("TRUNCATE \"UrlEntries\" ");
            }
            else if (db.Database.IsSqlite())
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM UrlEntries");
            }

            await cache.RemoveAllUrls();
        }

        public async Task<string?> GetLongUrl(string shortUrl)
        {
            var longUrl = await cache.GetLongUrl(shortUrl);

            if (!string.IsNullOrEmpty(longUrl))
            {
                return longUrl;
            }

            var entry = await db.UrlEntries.FirstOrDefaultAsync(s => s.ShortUrl == shortUrl);

            if (string.IsNullOrEmpty(longUrl) && !string.IsNullOrEmpty(entry?.LongUrl))
            {
                _ = Task.Run(() => cache.AddUrl(shortUrl, entry.LongUrl));
            }

            return entry?.LongUrl;
        }

        public Task EnsureCreated()
        {
            return db.Database.EnsureCreatedAsync();
        }
    }
}
