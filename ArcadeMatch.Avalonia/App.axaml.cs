using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Shared;
using GameFinder;

namespace ArcadeMatch.Avalonia;

public partial class App : Application
{
    public static ApiHandler Api = new();
    public static ApiSettings Settings { get; private set; } = new();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load configuration before connecting
            Settings = ApiSettings.Load();
            
            Task.Run(() => Api.Connect(Config.GameList.ToArray()));
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
