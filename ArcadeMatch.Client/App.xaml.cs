using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace GameFinder;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ApiHandler Api = new ApiHandler();
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
        ApplicationAccentColorManager.Apply(Color.FromRgb(79, 139, 255), ApplicationTheme.Dark);

        SplashScreen splash = new SplashScreen();
        splash.Show();

        await Api.Connect(Config.GameList.ToArray());

        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();

        await splash.CloseWithFadeAsync();
    }
}
