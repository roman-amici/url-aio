namespace UrlShortClient
{
    public class UrlCollection
    {
        public List<(string, string)> Urls { get; set; } = new();

        public IReadOnlyList<string> LongUrls { get; set; }

        public Random UrlRandom { get; set; } = new Random();
        public Random LongUrlRandom { get; set; } = new Random();

        private object urlsLock = new();
        private object longUrlLock = new();

        public UrlCollection(IReadOnlyList<string> longUrls)
        {
            LongUrls = longUrls;
        }

        public void AddUrl(string longUrl, string shortUrl)
        {
            lock (urlsLock)
            {
                Urls.Add((longUrl, shortUrl));
            }
        }

        public (string, string) GetRandomUrlPair()
        {
            lock (urlsLock)
            {
                // Use uniform for now.
                var index = UrlRandom.Next(0, Urls.Count);
                return Urls[index];
            }
        }

        public void DeleteUrl(string shortUrl)
        {
            lock (urlsLock)
            {
                for (var i = 0; i < Urls.Count; i++)
                {
                    var (_, shortUrlData) = Urls[i];

                    if (shortUrlData == shortUrl)
                    {
                        Urls.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public string GetRandomLongUrl()
        {
            lock (longUrlLock)
            {
                // Lock is for random and not the collection
                var index = LongUrlRandom.Next(0, LongUrls.Count);
                return LongUrls[index];
            }
        }
    }
}
