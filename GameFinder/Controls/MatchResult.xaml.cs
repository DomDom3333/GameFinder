using System.Windows;
using System.Windows.Controls;

namespace GameFinder.Controls;

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

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke();
    }
}
