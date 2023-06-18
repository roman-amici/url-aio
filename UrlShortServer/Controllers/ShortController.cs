using Microsoft.AspNetCore.Mvc;
using UrlShortServer.Database;
using UrlShortServer.Services;
using UrlShortServer.Transport;

namespace UrlShortServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShortController : ControllerBase
    {

        IShortenerService ShortenerService { get; set; }
        UrlDbContext Db { get; set; }

        IConfiguration Config { get; set; }

        public ShortController(IShortenerService service, UrlDbContext db, IConfiguration config)
        {
            ShortenerService = service;
            Db = db;
            Config = config;
        }

        [HttpGet("{shortUrl}")]
        public async Task<IActionResult> Get(string shortUrl)
        {
            var longUrl = await ShortenerService.GetLongUrl(shortUrl);

            if (string.IsNullOrEmpty(longUrl))
            {
                return NotFound();
            }
            else
            {
                return Redirect(longUrl);
            }
        }

        [HttpDelete("{shortUrl}")]
        public async Task<IActionResult> DeleteEntry(string shortUrl)
        {
            var result = await ShortenerService.DeleteUrl(shortUrl);

            if (result)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ShortenerRequest request)
        {
            var result = await ShortenerService.AddUrl(request.BaseUrl);

            return Ok(result);
        }

        [HttpDelete]
        public async Task Delete()
        {
            await ShortenerService.DeleteAllUrls();
        }

        [HttpPost("table")]
        public Task Post()
        {
            return Db.Database.EnsureCreatedAsync();
        }

        [HttpGet("table")]
        public ConfigurationResponse Get()
        {
            return new ConfigurationResponse()
            {
                CacheType = Config.GetValue<string>("CacheType"),
                DatabaseType = Config.GetValue<string>("DbType"),
                ServerString = Config.GetValue<string>("ServerString")
            };
        }

    }
}
