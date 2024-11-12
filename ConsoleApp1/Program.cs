using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using Cookie = OpenQA.Selenium.Cookie;

class Program
{
    static readonly string SteamLoginUrl = "https://store.steampowered.com/login/";
    static readonly string UserDataUrl = "https://store.steampowered.com/dynamicstore/userdata/";
    static readonly string cookiesFilePath = "cookies.json";

    static async Task Main(string[] args)
    {
        IWebDriver driver = new ChromeDriver(); // Set default browser as Chrome

        IReadOnlyCollection<Cookie>? cookies = LoadCookies(cookiesFilePath);
        if (cookies == null)
        {
            cookies = await PromptUserToLogin(driver);
        }

        try
        {
            string jsonResponse = await FetchJsonResponseWithCookies(cookies);
            JsonNode? json = JsonNode.Parse(jsonResponse);
            JsonNode? rgOwnedPackages = json?["rgOwnedPackages"];

            if (rgOwnedPackages is JsonArray packages && packages.Count > 0)
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "steam_data.json");
                await File.WriteAllTextAsync(filePath, jsonResponse);
                Console.WriteLine($"JSON data has been saved to: {filePath}");
            }
            else
            {
                Console.WriteLine("Failed to retrieve valid data. Please log into Steam.");
                cookies = await PromptUserToLogin(driver);
                jsonResponse = await FetchJsonResponseWithCookies(cookies);
                json = JsonNode.Parse(jsonResponse);
                rgOwnedPackages = json?["rgOwnedPackages"];

                if (rgOwnedPackages is JsonArray secondAttemptPackages && secondAttemptPackages.Count > 0)
                {
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "steam_data.json");
                    await File.WriteAllTextAsync(filePath, jsonResponse);
                    Console.WriteLine($"JSON data has been saved to: {filePath}");
                }
                else
                {
                    Console.WriteLine("Failed to retrieve valid data even after log in. Please try again later.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    static async Task<IReadOnlyCollection<Cookie>> PromptUserToLogin(IWebDriver driver)
    {
        try
        {
            driver.Navigate().GoToUrl(SteamLoginUrl);
            Console.WriteLine("Press Enter after you have successfully logged in.");
            Console.ReadLine();

            ReadOnlyCollection<Cookie>? cookies = driver.Manage().Cookies.AllCookies;
            SaveCookies(cookiesFilePath, cookies);
            return cookies;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
            return null;
        }
        finally
        {
            driver?.Quit();
        }
    }

    static async Task<string> FetchJsonResponseWithCookies(IReadOnlyCollection<Cookie> cookies)
    {
        Uri baseUri = new Uri(UserDataUrl);
        using HttpClientHandler handler = new HttpClientHandler { CookieContainer = new CookieContainer() };

        foreach (Cookie cookie in cookies)
        {
            string encodedValue = HttpUtility.UrlEncode(cookie.Value, Encoding.UTF8);
            handler.CookieContainer.Add(baseUri,
                new System.Net.Cookie(cookie.Name, encodedValue, cookie.Path, cookie.Domain));
        }

        using HttpClient client = new HttpClient(handler);
        return await client.GetStringAsync(baseUri);
    }

    static void SaveCookies(string path, IReadOnlyCollection<Cookie> cookies)
    {
        List<CookieData> cookieDataList = new List<CookieData>();
        foreach (Cookie cookie in cookies)
        {
            cookieDataList.Add(new CookieData(cookie));
        }

        string json = JsonSerializer.Serialize(cookieDataList, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    static IReadOnlyCollection<Cookie>? LoadCookies(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        List<CookieData>? cookieDataList = JsonSerializer.Deserialize<List<CookieData>>(json);
        if (cookieDataList == null)
        {
            return null;
        }

        List<Cookie> cookies = new List<Cookie>();
        foreach (CookieData cookieData in cookieDataList)
        {
            cookies.Add(cookieData.ToSeleniumCookie());
        }

        return cookies;
    }
}

public class CookieData
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public DateTime? Expiry { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public string SameSite { get; set; }

    public CookieData()
    {
    }

    public CookieData(Cookie cookie)
    {
        Name = cookie.Name;
        Value = cookie.Value;
        Domain = cookie.Domain;
        Path = cookie.Path;
        Expiry = cookie.Expiry;
        Secure = cookie.Secure;
        HttpOnly = cookie.IsHttpOnly;
        SameSite = cookie.SameSite;
    }

    public Cookie ToSeleniumCookie()
    {
        return new Cookie(Name, Value, Domain, Path, Expiry, Secure, HttpOnly, SameSite);
    }
}