using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArcadeMatch.Avalonia.ViewModels;
using ArcadeMatch.Avalonia.ViewModels.Tabs;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Tabs : UserControl
{
    private readonly TabsViewModel _viewModel;

    public Tabs()
    {
        InitializeComponent();
        _viewModel = new TabsViewModel(App.SteamGameService, App.UserConfig, App.Api, App.Settings);
        DataContext = _viewModel;
        _viewModel.Home.MessageRequested += OnMessageRequested;
        _viewModel.MessageRequested += OnMessageRequested;
    }

    public override async void EndInit()
    {
        base.EndInit();
        await _viewModel.InitializeAsync();
    }

    private void OnMessageRequested(object? sender, MessageRequestedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => _ = ShowMessageAsync(e.Title, e.Message));
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        if (this.GetVisualRoot() is Window window)
        {
            await App.DialogService.ShowMessageAsync(window, title, message);
        }
    }
}
