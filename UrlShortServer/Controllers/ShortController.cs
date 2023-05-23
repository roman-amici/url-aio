using Microsoft.AspNetCore.Mvc;
using UrlShortServer.Services;
using UrlShortServer.Transport;

namespace UrlShortServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShortController : ControllerBase
    {

        IShortenerService ShortenerService { get; set; }

        public ShortController(IShortenerService service ) 
        {
            ShortenerService = service;
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

    }
}
