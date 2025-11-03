using Avalonia;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadeMatch.Avalonia;

static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeServices(ConfigureServices());
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApiSettings>(_ => ApiSettings.Load());
        services.AddSingleton<IUserConfigStore, UserConfigStore>();
        services.AddSingleton<ISteamGameService, SteamGameService>();
        services.AddSingleton<ISessionApi, ApiHandler>();
        services.AddSingleton<IDialogService, DialogService>();
        return services.BuildServiceProvider();
    }
}
