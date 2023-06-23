// See https://aka.ms/new-console-template for more information


using UrlShortClient;
using UrlShortServer.Transport;

async Task RunSimulation(int numClients, string baseUrl)
{

    var duration = TimeSpan.FromSeconds(60);

    var longUrls = new List<string>()
    {
        "https://google.com",
        "https://reddit.com",
        "https://cnn.com",
        "https://news.ycombinator.com",
        "https://whatever.com"
    };

    var readMagnitude = 100.0;
    var writeMagnitude = 10.0;
    var deleteMagnitude = 1.0;

    var clients = new List<UrlShortClient.UrlShortClient>();

    var urlCollection = new UrlCollection(longUrls);
    var aggregator = new Aggregator();

    ConfigurationResponse? config;
    using (var client = new HttpClient())
    {
        Console.WriteLine("Getting database description");
        config = await UrlShortClient.UrlShortClient.Describe(baseUrl, client);

        if (config == null)
        {
            Console.WriteLine("Failed to retrieve configuration.");
        }
        else
        {
            Console.WriteLine("Configuration received:");
            Console.WriteLine($"Database: {config?.DatabaseType}");
            Console.WriteLine($"Cache: {config?.CacheType}");
            Console.WriteLine($"ServerString : {config?.ServerString}");
        }

        Console.WriteLine("Creating database");
        await UrlShortClient.UrlShortClient.EnsureCreated(baseUrl, client);

        Console.WriteLine("Clearing database");
        await UrlShortClient.UrlShortClient.Reset(baseUrl, client);
    }

    var total = (readMagnitude + writeMagnitude + deleteMagnitude);
    var readProbability = readMagnitude / total;
    var writeProbability = writeMagnitude / total;
    var deleteProbability = deleteMagnitude / total;

    var scenario = () =>
    {
        var random = new Random().NextDouble();

        // Assume read > write > delete

        if (random < deleteProbability)
        {
            return UrlCommand.Delete;
        }
        else if (random < deleteProbability + writeProbability)
        {
            return UrlCommand.Write;
        }
        else
        {
            return UrlCommand.Read;
        }
    };

    Console.WriteLine("Beginning Test with:");
    Console.WriteLine($"NumClients: {numClients}");

    Console.WriteLine("Creating clients.");
    for (var i = 0; i < numClients; i++)
    {
        clients.Add(new UrlShortClient.UrlShortClient(i, baseUrl, scenario, aggregator, urlCollection));
    }

    Console.WriteLine("Adding initial entries");
    // Make sure there's some url's already in the database so we don't get 404's on the read
    for (var i = 0; i < Math.Ceiling((1 - readProbability) * numClients); i++)
    {
        await clients[0].Write();
    }

    var cancellationTokens = new CancellationTokenSource();

    var clientTasks = new List<Task>();

    Console.WriteLine("Starting clients");
    foreach (var client in clients)
    {
        clientTasks.Add(client.RunUntilCancellation(cancellationTokens.Token));
        await Task.Delay(10);
    }

    Console.WriteLine("All clients started. Starting test");
    aggregator.StartRecording = true;

    await Task.Delay(duration);

    Console.WriteLine("Test complete");

    cancellationTokens.Cancel();

    await Task.WhenAny(Task.WhenAll(clientTasks), Task.Delay(TimeSpan.FromSeconds(30)));

    var reads = aggregator.SummarizeReadResponses();

    var failures = aggregator.ReadEntries.Where(x => !x.Success).ToList().Count;

    var avg = TimeSpan.FromMilliseconds(reads.Select(x => x.TotalMilliseconds).Average());
    var readsPerSecond = reads.Count / duration.TotalSeconds;
    var p50 = Aggregator.Quantile(reads, 0.5);
    var p99 = Aggregator.Quantile(reads, 0.99);


    Console.WriteLine("\nResults:");
    Console.WriteLine($"Database: {config?.DatabaseType}");
    Console.WriteLine($"Cache: {config?.CacheType}");
    Console.WriteLine($"ServerString : {config?.ServerString}");
    Console.WriteLine($"NumClients: {numClients}");
    Console.WriteLine($"Read-Write-Delete: {readMagnitude}-{writeMagnitude}-{deleteMagnitude}");
    Console.WriteLine($"Duration: {duration.TotalSeconds}");
    Console.WriteLine($"Reads/s {readsPerSecond}");
    Console.WriteLine($"Failure Percentage {failures / (reads.Count + failures)}");
    Console.WriteLine($"Average {avg.TotalMilliseconds} ms");
    Console.WriteLine($"p50 {p50.TotalMilliseconds} ms");
    Console.WriteLine($"p99 {p99.TotalMilliseconds} ms");

    Console.WriteLine("\n\n");
}

var cmd = Environment.GetCommandLineArgs().ToArray();
var baseUrl = "https://raa-url-short.azurewebsites.net";
if (cmd.Length >= 2)
{
    baseUrl = cmd[1];
}

Console.WriteLine($"BaseUrl: {baseUrl}");

foreach (var numClients in new[] { 10, 100, 250, 500 }) //  100, 500, 1000, 2500, 5000, 10000 })
{
    await RunSimulation(numClients, baseUrl);
}


