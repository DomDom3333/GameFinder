using System.Windows;
using System.Windows.Controls;
using OpenQA.Selenium;

namespace GameFinder.Controls;

public partial class SessionStart : UserControl
{
    public SessionStart()
    {
        InitializeComponent();
    }

    private async void StartNewSession_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Config.Username))
        {
            MessageBox.Show("Please enter a valid Username", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        await App.Api.CreateSessionAsync();
        while (string.IsNullOrWhiteSpace(App.Api.SessionId))
        {
            await Task.Delay(200);
        }

        await App.Api.JoinSessionAsync(App.Api.SessionId, Config.Username, Config.GameList);
    }

    private async void JoinSession_OnClick(object sender, RoutedEventArgs e)
    {
        if (SessionCodeBox.Text.Length != 4)
        {
            MessageBox.Show("Please enter a valid Session Code", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (string.IsNullOrWhiteSpace(Config.Username))
        {
            MessageBox.Show("Please enter a valid Username", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        await App.Api.JoinSessionAsync(SessionCodeBox.Text, Config.Username, Config.GameList);
    }

    private void SessionCodeBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        SessionCodeBox.Text = string.Empty;
    }

    private void SessionCodeBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (SessionCodeBox.Text.Length == 0)
        {
            SessionCodeBox.Text = "_ _ _ _";
        }
    }

    private void DisplaynameBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        DisplaynameBox.Text = string.Empty;
    }

    private void DisplaynameBox_OnLostFocus(object sender, RoutedEventArgs e)
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
}