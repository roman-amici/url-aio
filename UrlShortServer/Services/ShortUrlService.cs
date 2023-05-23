
using Microsoft.AspNetCore.Authentication;

namespace UrlShortServer.Services
{
    public interface IShortUrlService
    {
        public Task<string> GenerateShortUrl();
    }

    public class RandomShortUrlService : IShortUrlService
    {
        public Task<string> GenerateShortUrl()
        {
            var random = new Random();
            var i1 = random.NextInt64();

            var bytes1 = BitConverter.GetBytes(i1);

            return Task.FromResult( Base64UrlTextEncoder.Encode(bytes1) );
        }
    }
}
