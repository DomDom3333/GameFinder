using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArcadeMatch.Avalonia.Controls;

public partial class MatchResult : UserControl
{
    public event Action? BackClicked;

    public MatchResult(string? gameId)
    {
        InitializeComponent();
        ResultText.Text = string.IsNullOrEmpty(gameId)
            ? "No common game found"
            : $"Play together: {gameId}";
    }

    void Back_Click(object? sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke();
    }
}
