using Microsoft.EntityFrameworkCore;

namespace UrlShortServer.Database
{
    public class UrlDbContext : DbContext
    {
        public UrlDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<UrlEntry> UrlEntries => Set<UrlEntry>();
       
    }
}
