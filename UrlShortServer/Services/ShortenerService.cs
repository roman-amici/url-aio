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
    }

    public class ShortenerService : IShortenerService
    {
        IShortUrlService shortUrlGenerator;
        UrlDbContext db;

        public ShortenerService(IShortUrlService shortGenerator, UrlDbContext db)
        {
            shortUrlGenerator = shortGenerator;
            this.db = db;
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

            return true;
        }

        public Task DeleteAllUrls()
        {
            return db.Database.ExecuteSqlRawAsync("DELETE FROM UrlEntries");
        }

        public async Task<string?> GetLongUrl(string shortUrl)
        {
            var entry = await db.UrlEntries.FirstOrDefaultAsync(s => s.ShortUrl == shortUrl);

            return entry?.LongUrl;
        }
    }
}
