using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ArcadeMatch.Avalonia.Services;
using Avalonia.VisualTree;
using GameFinder.Objects;
using OpenQA.Selenium;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Tabs : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private readonly ISteamGameService _steamGameService;

    public bool IsLoggedIn
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoggedIn)));
                UpdateConnectionStatusUi();
            }
        }
    }

    public Tabs()
    {
        _steamGameService = App.SteamGameService;
        InitializeComponent();
        DataContext = this;
        ShowSessionStart();
        App.Api.SessionEnded += OnSessionEnded;
    }

    public override async void EndInit()
    {
        base.EndInit();
        if (_steamGameService.HasSavedCookies())
        {
            await UpdateStatus(_steamGameService.LoadCookies() ?? throw new InvalidOperationException());
        }
        IsLoggedIn = await TryGetGameListAsync();
    }

    async void LoginButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ShowLogin();
        }
        catch (Exception )
        {
            // Dont know
        }
    }

    private async Task ShowLogin()
    {
        IReadOnlyCollection<Cookie> cookies = _steamGameService.PromptUserToLogin();
        if (cookies != null &&  cookies.Any())
        {
            await UpdateStatus(cookies);
        }
    }

    async void ApiFetchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        App.UserConfig.SteamApiKey = ApiKeyBox.Text?.Trim() ?? string.Empty;
        App.UserConfig.SteamId = SteamIdBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(App.UserConfig.SteamApiKey) || string.IsNullOrWhiteSpace(App.UserConfig.SteamId))
        {
            var window = this.GetVisualRoot() as Window;
            if (window != null)
                await App.DialogService.ShowMessageAsync(window, "Error", "Please enter both API key and Steam ID.");
            return;
        }

        var games = await _steamGameService.GetOwnedGamesViaApiAsync(App.UserConfig.SteamApiKey, App.UserConfig.SteamId ?? string.Empty);
        if (games != null)
        {
            App.UserConfig.GameList = games;
            IsLoggedIn = games.Count > 0;
        }
        else
        {
            IsLoggedIn = false;
        }
    }

    async void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await _steamGameService.GetOwnedAndWishlistGamesAsync();

        if (result == null)
        {
            var window = this.GetVisualRoot() as Window;
            if (window != null)
                await App.DialogService.ShowMessageAsync(window, "Error", "Failed to retrieve cookies. Please log into Steam.");
            return;
        }

        App.UserConfig.GameList = result.Value.OwnedGames;
        App.UserConfig.WishlistGames = result.Value.WishlistGames;
        IsLoggedIn = result.Value.OwnedGames.Count > 0;
    }

    async Task<bool> TryGetGameListAsync()
    {
        try
        {
            var result = await _steamGameService.GetOwnedAndWishlistGamesAsync();

            if (result != null)
            {
                App.UserConfig.GameList = result.Value.OwnedGames;
                App.UserConfig.WishlistGames = result.Value.WishlistGames;
                return result.Value.OwnedGames.Count > 0;
            }
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    void OnSessionButtonClicked(object? sender, string action)
    {
        switch (action)
        {
            case "StartNewSession":
            case "JoinSession":
                ShowSessionLobby();
                break;
            case "StartButton":
                ShowSwiping();
                break;
            case "LeaveButton":
                ShowSessionStart();
                break;
        }
    }

    private void ShowSessionStart()
    {
        var control = new SessionStart();
        control.SessionButtonClicked += OnSessionButtonClicked;
        SessionContentControl.Content = control;
    }

    private void ShowSessionLobby()
    {
        var control = new SessionLobby();
        control.StartButtonClicked += OnSessionButtonClicked;
        SessionContentControl.Content = control;
    }

    private void ShowSwiping()
    {
        var swiping = new Swiping();
        swiping.LeaveClicked += () => ShowSessionStart();
        SessionContentControl.Content = swiping;
    }

    private void ShowResults(IReadOnlyList<MatchedGame> games)
    {
        var result = new MatchResult(games);
        result.BackClicked += () => ShowSessionStart();
        SessionContentControl.Content = result;
    }

    private void OnSessionEnded(IReadOnlyList<MatchedGame> games)
    {
        Dispatcher.UIThread.Post(() => ShowResults(games));
    }

    private void UpdateConnectionStatusUi()
    {
        if (StatusBorder != null && StatusTextBlock != null)
        {
            if (IsLoggedIn)
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#44AA44"));
                StatusTextBlock.Text = "Connected";
            }
            else
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#FF4444"));
                StatusTextBlock.Text = "Not Connected";
            }
        }
    }

    private async Task UpdateStatus(IReadOnlyCollection<Cookie> cookies)
    {
        _steamGameService.ParseCookiesForData(cookies);
        var steamId = App.UserConfig.SteamId;
        App.UserConfig.UserProfile = steamId != null ? await SteamProfileFetcher.GetProfileAsync(steamId) : null;
        App.UserConfig.Username = App.UserConfig.UserProfile?.SteamId ?? string.Empty;
    }
}

