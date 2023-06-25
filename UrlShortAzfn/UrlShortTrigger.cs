
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
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(UrlShort.AzFn.Startup))]

namespace UrlShort.AzFn
{

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;
            builder.Services.AddDbContext<UrlDbContext>(options =>
            {
                options.UseNpgsql(config["DbConnectionString"]);
            });
            builder.Services.AddSingleton<ICacheService, NullCacheService>();
            builder.Services.AddScoped<IShortenerService, ShortenerService>();
            builder.Services.AddSingleton<IShortUrlService, RandomShortUrlService>();
        }
    }

    public class UrlShortTrigger
    {
        private IShortenerService ShortenerService { get; set; }
        private string ServerString { get; set; }

        public UrlShortTrigger(IShortenerService db, IConfiguration config)
        {
            ShortenerService = db;
            ServerString = config["ServerString"];
        }

        [FunctionName("AddUrl")]
        public async Task<IActionResult> AddUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Short")] HttpRequest req)
        {
            var jsonString = await req.ReadAsStringAsync();
            var x = JsonConvert.DeserializeObject<ShortenerRequest>(jsonString);

            var result = await ShortenerService.AddUrl(x.BaseUrl);

            return new OkObjectResult(result);
        }

        [FunctionName("GetLongUrl")]
        public async Task<IActionResult> GetLongUrl(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Short/{shortUrl}")] HttpRequest req,
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Short/{shortUrl}")] HttpRequest req,
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Short/table")] HttpRequest req
        )
        {
            await ShortenerService.EnsureCreated();
            return new OkResult();
        }

        [FunctionName("DeleteAllUrls")]
        public async Task<IActionResult> DeleteAllUrls(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Short")] HttpRequest req
        )
        {
            await ShortenerService.DeleteAllUrls();
            return new OkResult();
        }

        [FunctionName("GetConfiguration")]
        public async Task<IActionResult> GetConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Short/table")] HttpRequest req
        )
        {
            var configResponse = new ConfigurationResponse()
            {
                CacheType = "None",
                DatabaseType = "Postgres",
                ServerString = ServerString
            };

            return new OkObjectResult(configResponse);
        }
    }
}
