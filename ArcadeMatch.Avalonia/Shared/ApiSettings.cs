using System.IO;
using Microsoft.Extensions.Configuration;

namespace ArcadeMatch.Avalonia;

public class ApiSettings
{
    public ServerSettings Server { get; private set; } = new();

    public static ApiSettings Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        
        var appSettings = new ApiSettings();
        var serverSettings = configuration.GetSection("ServerSettings").Get<ServerSettings>() ?? new ServerSettings();
        appSettings.Server = serverSettings;
        
        return appSettings;
    }
}

public class ServerSettings
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5170";
    public string HubPath { get; set; } = "/matchinghub";
    public string SteamMarketDataPath { get; set; } = "/SteamMarketData/";

    public string HubUrl => $"{BaseUrl}{HubPath}";
    public string SteamMarketDataUrl => $"{BaseUrl}{SteamMarketDataPath}";
}

