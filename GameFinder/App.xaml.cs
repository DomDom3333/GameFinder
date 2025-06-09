using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using System.Windows;
using GameFinder.Objects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Cookie = OpenQA.Selenium.Cookie;

namespace GameFinder;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ApiHandler Api = new ApiHandler();
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        SplashScreen splash = new SplashScreen();
        splash.Show();

        await Api.Connect(Config.GameList.ToArray());

        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();

        await splash.CloseWithFadeAsync();
    }
}
