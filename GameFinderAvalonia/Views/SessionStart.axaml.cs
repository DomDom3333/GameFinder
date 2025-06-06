using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace GameFinderAvalonia.Views;

public partial class SessionStart : UserControl
{
    public event EventHandler? SessionAction;

    public SessionStart()
    {
        InitializeComponent();
    }

    private async void StartNewSession(object? sender, RoutedEventArgs e)
    {
        Config.Username = NameBox.Text ?? string.Empty;
        await App.Api.CreateSessionAsync();
        await App.Api.JoinSessionAsync(App.Api.SessionId, Config.Username, Config.GameList);
        SessionAction?.Invoke(this, EventArgs.Empty);
    }

    private async void JoinSession(object? sender, RoutedEventArgs e)
    {
        Config.Username = NameBox.Text ?? string.Empty;
        await App.Api.JoinSessionAsync(SessionCode.Text ?? string.Empty, Config.Username, Config.GameList);
        SessionAction?.Invoke(this, EventArgs.Empty);
    }
}
