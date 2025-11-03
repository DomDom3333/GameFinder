using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GameFinder.Objects;
using ArcadeMatch.Avalonia.ViewModels.Tabs;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Tabs : UserControl
{
    private readonly TabsViewModel _viewModel;

    public Tabs()
    {
        _viewModel = new TabsViewModel(App.SteamGameService, App.UserConfig);
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApiKeyBox.Text = _viewModel.SteamApiKey;
        SteamIdBox.Text = _viewModel.SteamId;
        UpdateConnectionStatusUi();
        ShowSessionStart();
        App.Api.SessionEnded += OnSessionEnded;
    }

    public override async void EndInit()
    {
        base.EndInit();
        await _viewModel.InitializeAsync();
        UpdateConnectionStatusUi();
    }

    async void LoginButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _viewModel.LoginAsync();
            if (!result.Success && result.ErrorMessage != null)
            {
                await ShowMessageAsync("Error", result.ErrorMessage);
            }
        }
        catch (Exception)
        {
            // Dont know
        }
        finally
        {
            UpdateConnectionStatusUi();
        }
    }

    async void ApiFetchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await _viewModel.FetchGamesViaApiAsync(ApiKeyBox.Text ?? string.Empty, SteamIdBox.Text ?? string.Empty);
        if (!result.Success)
        {
            await ShowMessageAsync("Error", result.ErrorMessage ?? "Unknown error occurred.");
        }
        UpdateConnectionStatusUi();
    }

    async void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await _viewModel.RefreshGamesAsync();
        if (!result.Success)
        {
            await ShowMessageAsync("Error", result.ErrorMessage ?? "Failed to refresh games.");
        }
        UpdateConnectionStatusUi();
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
        swiping.LeaveClicked += ShowSessionStart;
        SessionContentControl.Content = swiping;
    }

    private void ShowResults(IReadOnlyList<MatchedGame> games)
    {
        var result = new MatchResult(games);
        result.BackClicked += ShowSessionStart;
        SessionContentControl.Content = result;
    }

    private void OnSessionEnded(IReadOnlyList<MatchedGame> games)
    {
        Dispatcher.UIThread.Post(() => ShowResults(games));
    }

    private void UpdateConnectionStatusUi()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateConnectionStatusUi);
            return;
        }

        if (StatusBorder != null && StatusTextBlock != null)
        {
            if (_viewModel.IsLoggedIn)
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#44AA44"));
                StatusTextBlock.Text = _viewModel.SteamStatusText;
            }
            else
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#FF4444"));
                StatusTextBlock.Text = _viewModel.SteamStatusText;
            }
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var window = this.GetVisualRoot() as Window;
        if (window != null)
        {
            await App.DialogService.ShowMessageAsync(window, title, message);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabsViewModel.IsLoggedIn) || e.PropertyName == nameof(TabsViewModel.SteamStatusText))
        {
            UpdateConnectionStatusUi();
        }
    }
}

