using Microsoft.EntityFrameworkCore;
using System;

namespace UrlShortServer.Database
{
    [Index(nameof(ShortUrl))]
    public class UrlEntry
    {
        public long Id { get; set; }
        public string ShortUrl { get; set; } = null!;
        public string LongUrl { get; set; } = null!;
    }
}
