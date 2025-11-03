using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.Home.MessageRequested += OnHomeMessageRequested;
        ShowSessionStart();
        App.Api.SessionEnded += OnSessionEnded;
    }

    public override async void EndInit()
    {
        base.EndInit();
        await _viewModel.InitializeAsync();
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
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
}
