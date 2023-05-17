
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
            var i2 = random.NextInt64();

            var bytes1 = BitConverter.GetBytes(i1);
            var bytes2 = BitConverter.GetBytes(i2);

            var bytesCombined = new byte[16];
            bytes1.CopyTo(bytesCombined, 0);
            bytes2.CopyTo(bytesCombined, 8);

            return Task.FromResult( Convert.ToBase64String(bytesCombined));
        }
    }
}
