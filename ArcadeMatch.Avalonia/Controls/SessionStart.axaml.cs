using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ArcadeMatch.Avalonia;
using ArcadeMatch.Avalonia.ViewModels.Tabs;

namespace ArcadeMatch.Avalonia.Controls;

public partial class SessionStart : UserControl
{
    public event EventHandler<string>? SessionButtonClicked;

    private readonly SessionStartViewModel _viewModel;

    public SessionStart()
    {
        InitializeComponent();
        _viewModel = new SessionStartViewModel(App.Api, App.UserConfig);
        DataContext = _viewModel;
        DisplaynameBox.Text = string.IsNullOrWhiteSpace(_viewModel.DisplayName) ? "Your Display-name" : _viewModel.DisplayName;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        DisplaynameBox.Text = string.IsNullOrWhiteSpace(_viewModel.DisplayName) ? "Your Display-name" : _viewModel.DisplayName;
    }

    void DisplaynameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        var text = DisplaynameBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            DisplaynameBox.Text = "Your Display-name";
            _viewModel.DisplayName = string.Empty;
        }
        else
        {
            _viewModel.DisplayName = text;
        }
    }

    async void StartNewSession_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.DisplayName = (DisplaynameBox.Text == "Your Display-name" ? string.Empty : DisplaynameBox.Text) ?? string.Empty;
        var result = await _viewModel.StartNewSessionAsync();
        if (!result.Success)
        {
            await ShowMessageAsync(result.ErrorMessage ?? "An unknown error occurred.");
            return;
        }

        if (result.Action != null)
        {
            SessionButtonClicked?.Invoke(this, result.Action);
        }
    }

    async void JoinSession_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.SessionCode = SessionCodeBox.Text ?? string.Empty;
        _viewModel.DisplayName = (DisplaynameBox.Text == "Your Display-name" ? string.Empty : DisplaynameBox.Text) ?? string.Empty;

        var result = await _viewModel.JoinSessionAsync();
        if (!result.Success)
        {
            await ShowMessageAsync(result.ErrorMessage ?? "An unknown error occurred.");
            return;
        }

        if (result.Action != null)
        {
            SessionButtonClicked?.Invoke(this, result.Action);
        }
    }

    private async Task ShowMessageAsync(string message)
    {
        if (this.GetVisualRoot() is Window owner)
        {
            await App.DialogService.ShowMessageAsync(owner, "Error", message);
        }
    }
}
