using Avalonia.Controls;
using ArcadeMatch.Avalonia.ViewModels.Sessions;

namespace ArcadeMatch.Avalonia.Controls;

public partial class MatchResult : UserControl
{
    private MatchResultViewModel? _viewModel;

    public MatchResult()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.CopyMatchesRequested -= OnCopyMatchesRequested;
        }

        _viewModel = DataContext as MatchResultViewModel;
        if (_viewModel != null)
        {
            _viewModel.CopyMatchesRequested += OnCopyMatchesRequested;
        }
    }

    private async void OnCopyMatchesRequested(object? sender, string matches)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(matches);
        }
        catch
        {
            // Ignore clipboard errors.
        }
    }
}
