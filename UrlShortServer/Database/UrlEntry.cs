using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UrlShortServer.Database
{
    [Index(nameof(ShortUrl))]
    public class UrlEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public long Id { get; set; }
        public string ShortUrl { get; set; } = null!;
        public string LongUrl { get; set; } = null!;
    }
}
