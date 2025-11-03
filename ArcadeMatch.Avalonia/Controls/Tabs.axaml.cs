using Avalonia.Controls;
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
        _viewModel = new TabsViewModel(App.SteamGameService, App.UserConfig, App.Api, App.Settings);
        DataContext = _viewModel;
        _viewModel.Home.MessageRequested += OnMessageRequested;
        _viewModel.MessageRequested += OnMessageRequested;

        InitializeComponent();
    }

    public override async void EndInit()
    {
        base.EndInit();
        var vm = _viewModel;
        if (vm != null)
        {
            await vm.InitializeAsync();
        }
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
