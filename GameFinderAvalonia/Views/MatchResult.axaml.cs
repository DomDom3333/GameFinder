using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace GameFinderAvalonia.Views;

public partial class MatchResult : UserControl
{
    public event EventHandler? BackClicked;

    public MatchResult(string? game)
    {
        InitializeComponent();
        Result.Text = string.IsNullOrEmpty(game) ? "No common game" : $"Play {game}";
    }

    private void Back(object? sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke(this, EventArgs.Empty);
    }
}
