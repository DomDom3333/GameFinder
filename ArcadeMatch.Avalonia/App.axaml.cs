using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadeMatch.Avalonia;

public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static ISessionApi Api { get; private set; } = null!;
    public static IUserConfigStore UserConfig { get; private set; } = null!;
    public static ISteamGameService SteamGameService { get; private set; } = null!;
    public static IDialogService DialogService { get; private set; } = null!;
    public static ApiSettings Settings { get; private set; } = new();

    public static void InitializeServices(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
        Api = serviceProvider.GetRequiredService<ISessionApi>();
        UserConfig = serviceProvider.GetRequiredService<IUserConfigStore>();
        SteamGameService = serviceProvider.GetRequiredService<ISteamGameService>();
        DialogService = serviceProvider.GetRequiredService<IDialogService>();
        Settings = serviceProvider.GetRequiredService<ApiSettings>();
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Task.Run(() => Api.Connect(UserConfig.GameList.ToArray()));
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
