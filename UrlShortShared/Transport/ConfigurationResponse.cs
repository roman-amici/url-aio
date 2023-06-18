namespace UrlShortServer.Transport;

public class ConfigurationResponse
{
    public string DatabaseType { get; set; } = null!;
    public string CacheType { get; set; } = null!;
    public string ServerString { get; set; } = null!;
}