using System;
using Avalonia.Controls;
using ArcadeMatch.Avalonia.ViewModels.Sessions;

namespace ArcadeMatch.Avalonia.Controls;

public partial class SessionLobby : UserControl
{
    private SessionLobbyViewModel? _viewModel;

    public SessionLobby()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.CopyCodeRequested -= OnCopyCodeRequested;
        }

        _viewModel = DataContext as SessionLobbyViewModel;
        if (_viewModel != null)
        {
            _viewModel.CopyCodeRequested += OnCopyCodeRequested;
        }
    }

    private async void OnCopyCodeRequested(object? sender, string sessionId)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(sessionId);
        }
        catch
        {
            // Ignore clipboard errors.
        }
    }
}
