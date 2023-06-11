// See https://aka.ms/new-console-template for more information


using UrlShortClient;

async Task RunSimulation(int numClients)
{

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

    var readMagnitude = 100.0;
    var writeMagnitude = 10.0;
    var deleteMagnitude = 1.0;

    Console.WriteLine("Beginning Test with:");
    Console.WriteLine($"NumClients: {numClients}");

    var clients = new List<UrlShortClient.UrlShortClient>();

    var urlCollection = new UrlCollection(longUrls);
    var aggregator = new Aggregator();

    Console.WriteLine("Clearing database");
    await UrlShortClient.UrlShortClient.Reset(baseUrl);

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

    await Task.WhenAll(clientTasks);

    var reads = aggregator.SummarizeReadResponses();

    var failures = aggregator.ReadEntries.Where(x => !x.Success).ToList().Count;

    var avg = TimeSpan.FromMilliseconds(reads.Select(x => x.TotalMilliseconds).Average());
    var readsPerSecond = reads.Count / duration.TotalSeconds;
    var p50 = Aggregator.Quantile(reads, 0.5);
    var p99 = Aggregator.Quantile(reads, 0.99);


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

foreach (var numClients in new[] { 5000, 10000, 15000 })
{
    await RunSimulation(numClients);
}


