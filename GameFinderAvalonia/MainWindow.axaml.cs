using Avalonia.Controls;
using GameFinderAvalonia.Views;

namespace GameFinderAvalonia;

public partial class MainWindow : Window
{
    private Tabs _tabs;
    public MainWindow()
    {
        InitializeComponent();
        _tabs = this.FindControl<Tabs>("TabsControl");
        App.Api.SessionEnded += game => _tabs.ShowResults(game);
    }
}
