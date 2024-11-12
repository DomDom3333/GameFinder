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
using GameFinderApi.Objects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Cookie = OpenQA.Selenium.Cookie;

namespace GameFinderApi;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    readonly string SteamLoginUrl = "https://store.steampowered.com/login/";
    readonly string UserDataUrl = "https://store.steampowered.com/dynamicstore/userdata/";
    readonly string cookiesFilePath = "cookies.json";
    public static ApiHandler Api = new ApiHandler();
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        List<string> gameIds = await GetGamesList();
        Config.GameList.AddRange(gameIds);
        await Api.Connect(Config.GameList.ToArray());
        
        
        
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private async Task<List<string>> GetGamesList()
    {
        IReadOnlyCollection<Cookie> cookies = LoadCookies(cookiesFilePath) ?? await PromptUserToLogin();

        try
        {
            string jsonResponse = await FetchJsonResponseWithCookies(cookies);
            List<string> ownedPackages = ParseOwnedPackages(jsonResponse);
            if (ownedPackages.Count == 0)
            {
                Console.WriteLine("Failed to retrieve valid data. Please log into Steam.");
                cookies = await PromptUserToLogin();
                jsonResponse = await FetchJsonResponseWithCookies(cookies);
                ownedPackages = ParseOwnedPackages(jsonResponse);
            }

            PrintOwnedPackages(ownedPackages);
            return ownedPackages;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
            return new List<string>();
        }
    }

    async Task<IReadOnlyCollection<Cookie>> PromptUserToLogin()
    {
        using ChromeDriver driver = new ChromeDriver();
        try
        {
            driver.Navigate().GoToUrl(SteamLoginUrl);
            Console.WriteLine("Press Enter after you have successfully logged in.");
            Console.ReadLine();

            return driver.Manage().Cookies.AllCookies;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
            return null;
        }
        finally
        {
            SaveCookies(cookiesFilePath, driver.Manage().Cookies.AllCookies);
            driver.Quit();
        }
    }

    async Task<string> FetchJsonResponseWithCookies(IReadOnlyCollection<Cookie> cookies)
    {
        Uri baseUri = new Uri(UserDataUrl);
        using HttpClientHandler handler = new HttpClientHandler { CookieContainer = new CookieContainer() };

        foreach (Cookie cookie in cookies)
        {
            handler.CookieContainer.Add(baseUri, new System.Net.Cookie(
                cookie.Name, HttpUtility.UrlEncode(cookie.Value, Encoding.UTF8), cookie.Path, cookie.Domain)
            );
        }

        using HttpClient client = new HttpClient(handler);
        return await client.GetStringAsync(baseUri);
    }

    List<string> ParseOwnedPackages(string jsonResponse)
    {
        List<string> ownedPackagesList = new List<string>();
        JsonNode? json = JsonNode.Parse(jsonResponse)?["rgOwnedApps"];
        if (json is JsonArray packages)
        {
            foreach (JsonNode? package in packages)
            {
                ownedPackagesList.Add(package.ToString());
            }
        }

        return ownedPackagesList;
    }

    void PrintOwnedPackages(List<string> ownedPackagesList)
    {
        Console.WriteLine("Owned Packages:");
        foreach (string package in ownedPackagesList)
        {
            Console.WriteLine(package);
        }
    }

    static void SaveCookies(string path, IReadOnlyCollection<Cookie> cookies)
    {
        List<CookieData> cookieDataList = cookies.Select(cookie => new CookieData(cookie)).ToList();
        string json = JsonSerializer.Serialize(cookieDataList, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    static IReadOnlyCollection<Cookie>? LoadCookies(string path)
    {
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        List<CookieData>? cookieDataList = JsonSerializer.Deserialize<List<CookieData>>(json);

        return cookieDataList?.Select(cookieData => cookieData.ToSeleniumCookie()).ToList();
    }
}