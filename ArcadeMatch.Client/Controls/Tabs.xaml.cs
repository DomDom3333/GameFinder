using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using GameFinder.Objects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Cookie = OpenQA.Selenium.Cookie;

namespace GameFinder.Controls
{
    public partial class Tabs : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isLoggedIn;

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn != value)
                {
                    _isLoggedIn = value;
                    OnPropertyChanged(nameof(IsLoggedIn));
                }
            }
        }
        
        readonly string SteamLoginUrl = "https://store.steampowered.com/login/";
        readonly string cookiesFilePath = "cookies.json";
        readonly string UserDataUrl = "https://store.steampowered.com/dynamicstore/userdata/";

        public Tabs()
        {
            InitializeComponent();
            DataContext = this;
            ShowSessionStart();
            App.Api.SessionEnded += OnSessionEnded;
        }

        public async override void EndInit()
        {
            base.EndInit();
            IsLoggedIn = await TryGetGameListAsync();
            Console.WriteLine($"Owned packages: {Config.GameList.Count}");
        }

        private void ShowLogin()
        {
            IReadOnlyCollection<Cookie> cookies = PromptUserToLogin();
            SaveCookies(cookiesFilePath, cookies);
        }

        private void LoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowLogin();
        }

        private async void ApiFetchButton_OnClick(object sender, RoutedEventArgs e)
        {
            Config.SteamApiKey = ApiKeyBox.Text.Trim();
            Config.SteamId = SteamIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(Config.SteamApiKey) || string.IsNullOrWhiteSpace(Config.SteamId))
            {
                MessageBox.Show("Please enter both API key and Steam ID.");
                return;
            }

            IsLoggedIn = await TryGetGameListViaApiAsync(Config.SteamApiKey, Config.SteamId);
        }

        private async Task<bool> TryGetGameListAsync()
        {
            try
            {
                if (!File.Exists(cookiesFilePath))
                {
                    return false;
                }
                
                IReadOnlyCollection<Cookie>? cookies = LoadCookies(cookiesFilePath);
                if (cookies == null)
                {
                    return false;
                }
                
                string jsonResponse = await FetchJsonResponseWithCookies(cookies);
                
                List<string> ownedPackages = ParseOwnedPackages(jsonResponse);
                if (ownedPackages.Count < 1)
                {
                    return false;
                }

                Config.GameList = ownedPackages;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }

        private async Task<bool> TryGetGameListViaApiAsync(string apiKey, string steamId)
        {
            try
            {
                string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=0";
                using HttpClient client = new HttpClient();
                string jsonResponse = await client.GetStringAsync(url);

                JsonNode? games = JsonNode.Parse(jsonResponse)?["response"]?["games"];
                if (games is not JsonArray arr)
                {
                    return false;
                }

                List<string> ownedPackages = new();
                foreach (JsonNode? game in arr)
                {
                    string? id = game?["appid"]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        ownedPackages.Add(id);
                    }
                }

                Config.GameList = ownedPackages;
                return ownedPackages.Count > 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }

        private IReadOnlyCollection<Cookie> PromptUserToLogin()
        {
            using ChromeDriver driver = new ChromeDriver();
            try
            {
                driver.Navigate().GoToUrl(SteamLoginUrl);

                //Wait for user to log in
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(2));
                wait.Until(drv => drv.Url != SteamLoginUrl || drv.FindElements(By.Id("SpecificElementAfterLogin")).Count > 0);

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

        private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            var cookies = LoadCookies(cookiesFilePath);
            if (cookies == null)
            {
                throw new Exception("Failed to retrieve valid data. Please log into Steam.");
            }
            string jsonResponse = await FetchJsonResponseWithCookies(cookies);
            List<string> ownedPackages = ParseOwnedPackages(jsonResponse);
            Config.GameList = ownedPackages;
            Console.WriteLine($"Retrieved {ownedPackages.Count} owned packages.");
            
            IsLoggedIn = ownedPackages.Count > 0;
        }
        
        
        internal void ShowSessionStart()
        {
            var sessionStartControl = new SessionStart();
            sessionStartControl.SessionButtonClicked += OnSessionButtonClicked;
            SetContentAnimated(sessionStartControl);
        }
        
        private void OnSessionButtonClicked(object? sender, string action)
        {
            // Handle different actions
            if (action == "StartNewSession")
            {
                ShowSessionLobby();
            }
            else if (action == "JoinSession")
            {
                ShowSessionLobby();
            }
            else if (action == "StartButton")
            {
                ShowSwiping();
            }
            else if (action == "LeaveButton")
            {
                ShowSessionStart();
            }
        }

        internal void ShowSessionLobby()
        {
            var sessionLobbyCotrol = new SessionLobby();
            sessionLobbyCotrol.StartButtonClicked += OnSessionButtonClicked;
            SetContentAnimated(sessionLobbyCotrol);
        }

        internal void ShowSwiping()
        {
            var swiping = new Swiping();
            swiping.LeaveClicked += () => ShowSessionStart();
            SetContentAnimated(swiping);
        }

        internal void ShowResults(IReadOnlyList<MatchedGame> games)
        {
            var result = new MatchResult(games);
            result.BackClicked += () => ShowSessionStart();
            SetContentAnimated(result);
        }

        private void SetContentAnimated(UserControl control)
        {
            if (SessionContentControl.Content is UIElement oldContent)
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (_, _) =>
                {
                    SessionContentControl.Content = control;
                    control.Opacity = 0;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                    control.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                SessionContentControl.Content = control;
            }
        }

        private void OnSessionEnded(IReadOnlyList<MatchedGame> games)
        {
            Dispatcher.Invoke(() => ShowResults(games));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Console.WriteLine($"PropertyChanged: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}