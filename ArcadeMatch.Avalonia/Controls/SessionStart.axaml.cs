using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ArcadeMatch.Avalonia;
using ArcadeMatch.Avalonia.Services;

namespace ArcadeMatch.Avalonia.Controls;

public partial class SessionStart : UserControl
{
    public event EventHandler<string>? SessionButtonClicked;

    public SessionStart()
    {
        InitializeComponent();
        if (App.UserConfig.UserProfile != null)
        {
            DisplaynameBox.Text = App.UserConfig.UserProfile.SteamId;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (App.UserConfig.UserProfile != null)
        {
            DisplaynameBox.Text = App.UserConfig.UserProfile.SteamId;
        }
    }

    void DisplaynameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplaynameBox.Text))
        {
            DisplaynameBox.Text = "Your Display-name";
            App.UserConfig.Username = string.Empty;
        }
        else
        {
            App.UserConfig.Username = DisplaynameBox.Text;
        }
    }

    async void StartNewSession_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(App.UserConfig.Username))
        {
            if (this.GetVisualRoot() is Window owner)
            {
                await App.DialogService.ShowMessageAsync(owner, "Error", "Please enter a valid Username");
            }
            return;
        }
        await App.Api.CreateSessionAsync();
        while (string.IsNullOrWhiteSpace(App.Api.SessionId))
        {
            await Task.Delay(200);
        }
        await App.Api.JoinSessionAsync(App.Api.SessionId, App.UserConfig.Username, App.UserConfig.GameList, App.UserConfig.WishlistGames);
        SessionButtonClicked?.Invoke(this, "StartNewSession");
    }

    async void JoinSession_OnClick(object? sender, RoutedEventArgs e)
    {
        string? sessionCode = SessionCodeBox.Text;
        if (string.IsNullOrWhiteSpace(sessionCode) || sessionCode.Length != 4)
        {
            if (this.GetVisualRoot() is Window owner)
            {
                await App.DialogService.ShowMessageAsync(owner, "Error", "Please enter a valid Session Code");
            }
            return;
        }
        if (string.IsNullOrWhiteSpace(App.UserConfig.Username))
        {
            if (this.GetVisualRoot() is Window owner)
            {
                await App.DialogService.ShowMessageAsync(owner, "Error", "Please enter a valid Username");
            }
            return;
        }
        await App.Api.JoinSessionAsync(sessionCode, App.UserConfig.Username, App.UserConfig.GameList, App.UserConfig.WishlistGames);
        SessionButtonClicked?.Invoke(this, "JoinSession");
    }
}
