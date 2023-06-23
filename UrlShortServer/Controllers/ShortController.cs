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

        IConfiguration Config { get; set; }

        public ShortController(IShortenerService service, IConfiguration config)
        {
            ShortenerService = service;
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
        public async Task DeleteAllUrls()
        {
            await ShortenerService.DeleteAllUrls();
        }

        [HttpPost("table")]
        public Task EnsureCreated()
        {
            return ShortenerService.EnsureCreated();
        }

        [HttpGet("table")]
        public ConfigurationResponse GetConfiguration()
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
