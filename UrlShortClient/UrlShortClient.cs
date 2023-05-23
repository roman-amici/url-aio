using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UrlShortServer.Transport;

namespace UrlShortClient
{
    internal class UrlShortClient
    {

        public string Address { get; set; }
        public double ReadProbability { get; set; }
        public int ClientId { get; set; }

        public HttpClient HttpClient { get; set; }
        public Stopwatch Stopwatch { get; set; } = new Stopwatch();

        public Aggregator Aggregator { get; set; }

        public UrlCollection Urls { get; set; }

        public UrlShortClient(
            int clientId, 
            string address, 
            double readProbability, 
            Aggregator aggregator,
            UrlCollection urls)
        {
            ClientId = clientId;
            Address = address;
            ReadProbability = readProbability;
            Aggregator = aggregator;
            Urls = urls;

            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };

            HttpClient = new HttpClient(handler);
        }

        public static async Task Reset(string baseUrl)
        {
            using var client = new HttpClient();
            var response = await client.DeleteAsync($"{baseUrl}/short");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to delete database");
            }
        }

        public async Task RunUntilCancelation(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var random = new Random();

                    if (random.NextDouble() < ReadProbability)
                    {
                        await Read();
                    }
                    else
                    {
                        await Write();
                    }

                    await Task.Delay(50);
                }
                catch { }
            }
        }

        public async Task<bool> Read()
        {
            var (longUrl, shortUrl) = Urls.GetRandomUrlPair();

            Stopwatch.Restart();
            var response = await HttpClient.GetAsync($"{Address}/short/{shortUrl}");
            Stopwatch.Stop();

            bool success = response.StatusCode == HttpStatusCode.Redirect;

            if (success)
            {
                var redirect = response?.Headers?.Location?.OriginalString;
                if (redirect != longUrl)
                {
                    success = false;
                }
            }

            var entry = new Entry(ClientId, Kind.Read, Stopwatch.Elapsed, success);

            Aggregator.AddEntry(entry);


            return entry.Success;
        }

        public async Task<bool> Write()
        {
            var longUrl = Urls.GetRandomLongUrl();

            var contentJson = JsonSerializer.Serialize(new ShortenerRequest()
            {
                BaseUrl = longUrl,
            });
            var content = new StringContent(contentJson, Encoding.UTF8, "text/json");

            Stopwatch.Restart();
            var response = await HttpClient.PostAsync($"{Address}/short", content);
            Stopwatch.Stop();

            var success = response.StatusCode == HttpStatusCode.OK;

            var entry = new Entry(ClientId, Kind.Write, Stopwatch.Elapsed, success);
            Aggregator.AddEntry(entry);

            if (success)
            {
                var result = await response.Content.ReadFromJsonAsync<ShortenerResponse>();
                Urls.AddUrl(longUrl, result!.Message);
            }

            return entry.Success;
        }

    }
}
