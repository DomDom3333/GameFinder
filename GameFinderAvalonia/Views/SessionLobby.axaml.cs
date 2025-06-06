using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using GameFinderAvalonia.Helpers;

namespace GameFinderAvalonia.Views;

public partial class SessionLobby : UserControl
{
    private ObservableCollection<string> _users = new();
    public event EventHandler? StartClicked;

    public SessionLobby()
    {
        InitializeComponent();
        Users.ItemsSource = _users;
        CodeText.Text = $"Code: {App.Api.SessionId}";
        App.Api.UserJoinedSession += OnUserJoined;
        App.Api.UserLeftSession += OnUserLeft;
    }

    private void OnUserJoined(string user, bool admin)
    {
        UiHelper.SafeInvoke(() => _users.Add(user));
    }

    private void OnUserLeft(string user)
    {
        UiHelper.SafeInvoke(() => _users.Remove(user));
    }

    private async void StartSession(object? sender, RoutedEventArgs e)
    {
        await App.Api.StartSession(App.Api.SessionId);
        StartClicked?.Invoke(this, EventArgs.Empty);
    }

    private async void Leave(object? sender, RoutedEventArgs e)
    {
        await App.Api.LeaveSessionAsync(Config.Username);
        StartClicked?.Invoke(this, EventArgs.Empty); // go back
    }
}
