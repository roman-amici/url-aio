
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using UrlShortServer.Database;
using UrlShortServer.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UrlShortServer.Transport;

[assembly: FunctionsStartup(typeof(UrlShort.AzFn.Startup))]

namespace UrlShort.AzFn
{

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddDbContext<UrlDbContext>(options =>
            {
                options.UseNpgsql("Host=localhost; Database=urlshort; Username=db_user; Password=admin;Maximum Pool Size=1024");
            });
            builder.Services.AddSingleton<ICacheService, NullCacheService>();
            builder.Services.AddScoped<IShortenerService, ShortenerService>();
            builder.Services.AddSingleton<IShortUrlService, RandomShortUrlService>();
        }
    }

    public class UrlShortTrigger
    {
        private IShortenerService ShortenerService { get; set; }

        public UrlShortTrigger(IShortenerService db)
        {
            ShortenerService = db;
        }

        [FunctionName("AddUrl")]
        public async Task<IActionResult> AddUrl(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Short")] HttpRequest req)
        {
            var jsonString = await req.ReadAsStringAsync();
            var x = JsonConvert.DeserializeObject<ShortenerRequest>(jsonString);

            var result = await ShortenerService.AddUrl(x.BaseUrl);

            return new OkObjectResult(result);
        }

        [FunctionName("GetLongUrl")]
        public async Task<IActionResult> GetLongUrl(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Short/{shortUrl}")] HttpRequest req,
            string shortUrl)
        {
            var longUrl = await ShortenerService.GetLongUrl(shortUrl);

            if (string.IsNullOrEmpty(longUrl))
            {
                return new NotFoundResult();
            }
            else
            {
                return new RedirectResult(longUrl);
            }
        }

        [FunctionName("DeleteEntry")]
        public async Task<IActionResult> DeleteEntry(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Short/{shortUrl}")] HttpRequest req,
            string shortUrl)
        {
            var result = await ShortenerService.DeleteUrl(shortUrl);

            if (result)
            {
                return new OkResult();
            }
            else
            {
                return new NotFoundResult();
            }
        }


        [FunctionName("EnsureCreated")]
        public async Task<IActionResult> EnsureCreated(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Short/table")] HttpRequest req
        )
        {
            await ShortenerService.EnsureCreated();
            return new OkResult();
        }

        [FunctionName("DeleteAllUrls")]
        public async Task<IActionResult> DeleteAllUrls(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Short")] HttpRequest req
        )
        {
            await ShortenerService.DeleteAllUrls();
            return new OkResult();
        }

        [FunctionName("GetConfiguration")]
        public async Task<IActionResult> GetConfiguration(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Short/table")] HttpRequest req
        )
        {
            var configResponse = new ConfigurationResponse()
            {
                CacheType = "None",
                DatabaseType = "Postgres",
                ServerString = "Web=AzFn Local;Db=Local"
            };

            return new OkObjectResult(configResponse);
        }
    }
}
