using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GameFinder.Objects;
using ArcadeMatch.Avalonia.ViewModels.Tabs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Tabs : UserControl
{
    private readonly TabsViewModel _viewModel;

    public Tabs()
    {
        _viewModel = new TabsViewModel(App.SteamGameService, App.UserConfig);
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.Home.MessageRequested += OnHomeMessageRequested;
        ShowSessionStart();
        App.Api.SessionEnded += OnSessionEnded;
    }

    public override async void EndInit()
    {
        base.EndInit();
        await _viewModel.Home.InitializeAsync();
    }

    private void OnHomeMessageRequested(object? sender, MessageRequestedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => _ = ShowMessageAsync(e.Title, e.Message));
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

    private async Task ShowMessageAsync(string title, string message)
    {
        if (this.GetVisualRoot() is Window window)
            await App.DialogService.ShowMessageAsync(window, title, message);
    }
}
