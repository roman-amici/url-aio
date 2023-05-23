// See https://aka.ms/new-console-template for more information


using UrlShortClient;

var numClients = 100;
var readProbability = 0.95;

var duration = TimeSpan.FromSeconds(60);
var baseUrl = "https://localhost:7249";

var longUrls = new List<string>()
{
    "https://google.com",
    "https://reddit.com",
    "https://cnn.com",
    "https://news.ycombinator.com",
    "https://whatever.com"
};

Console.WriteLine("Beginning Test with:");
Console.WriteLine($"NumClients: {numClients}");
Console.WriteLine($"Read Probability: {readProbability}");
Console.WriteLine($"Duration: {duration.TotalSeconds}");

Console.WriteLine("");

var clients = new List<UrlShortClient.UrlShortClient>();

var urlCollection = new UrlCollection(longUrls);
var aggregator = new Aggregator();

Console.WriteLine("Clearing database");
await UrlShortClient.UrlShortClient.Reset(baseUrl);

Console.WriteLine("Creating clients.");
for(var i = 0; i < numClients; i++)
{
    clients.Add(new UrlShortClient.UrlShortClient(i, baseUrl, readProbability, aggregator, urlCollection));
}

Console.WriteLine("Adding initial entries");
// Make sure there's some url's already in the database so we don't get 404's on the read
for (var i = 0; i <  Math.Ceiling( (1 - readProbability) * numClients); i++)
{
    await clients[0].Write();
}

var cancelationTokens = new CancellationTokenSource();

var clientTasks = new List<Task>();

Console.WriteLine("Starting clients");
foreach (var client in clients)
{
    clientTasks.Add(client.RunUntilCancelation(cancelationTokens.Token));
    await Task.Delay(50);
}

Console.WriteLine("All clients started. Starting test");

await Task.Delay(duration);

Console.WriteLine("Test complete");

cancelationTokens.Cancel();

await Task.WhenAll(clientTasks);

var reads = aggregator.SummarizeReadResponses();

var failures = aggregator.ReadEntries.Where(x => !x.Success).ToList().Count;

var avg = TimeSpan.FromMilliseconds( reads.Select(x => x.TotalMilliseconds).Average());
var readsPerSecond = reads.Count / duration.TotalSeconds;
var p50 = Aggregator.Quantile(reads, 0.5);
var p99 = Aggregator.Quantile(reads, 0.99);

Console.WriteLine($"Reads/s {readsPerSecond}");
Console.WriteLine($"Failure Percentage {failures / (reads.Count + failures)}");
Console.WriteLine($"Average {avg.TotalMilliseconds} ms");
Console.WriteLine($"p50 {p50.TotalMilliseconds} ms");
Console.WriteLine($"p99 {p99.TotalMilliseconds} ms");

Console.ReadKey();
