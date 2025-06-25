using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MessageBox.Avalonia;
using ArcadeMatch.Avalonia;
using Avalonia.VisualTree;

namespace ArcadeMatch.Avalonia.Controls;

public partial class SessionStart : UserControl
{
    public event EventHandler<string>? SessionButtonClicked;

    public SessionStart()
    {
        InitializeComponent();
    }

    void DisplaynameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplaynameBox.Text))
        {
            DisplaynameBox.Text = "Your Display-name";
            Config.Username = string.Empty;
        }
        else
        {
            Config.Username = DisplaynameBox.Text;
        }
    }

    async void StartNewSession_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Config.Username))
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Please enter a valid Username").ShowDialog((Window)this.GetVisualRoot());
            return;
        }
        await App.Api.CreateSessionAsync();
        while (string.IsNullOrWhiteSpace(App.Api.SessionId))
        {
            await Task.Delay(200);
        }
        await App.Api.JoinSessionAsync(App.Api.SessionId, Config.Username, Config.GameList);
        SessionButtonClicked?.Invoke(this, "StartNewSession");
    }

    async void JoinSession_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SessionCodeBox.Text.Length != 4)
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Please enter a valid Session Code").ShowDialog((Window)this.GetVisualRoot());
            return;
        }
        if (string.IsNullOrWhiteSpace(Config.Username))
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Please enter a valid Username").ShowDialog((Window)this.GetVisualRoot());
            return;
        }
        await App.Api.JoinSessionAsync(SessionCodeBox.Text, Config.Username, Config.GameList);
        SessionButtonClicked?.Invoke(this, "JoinSession");
    }
}
