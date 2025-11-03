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
        // Create and assign ViewModel before InitializeComponent so EndInit (triggered by XAML loader)
        // can safely access it.
        _viewModel = new TabsViewModel(App.SteamGameService, App.UserConfig, App.Api, App.Settings);
        DataContext = _viewModel;
        _viewModel.Home.MessageRequested += OnMessageRequested;
        _viewModel.MessageRequested += OnMessageRequested;

        InitializeComponent();
    }

    public override async void EndInit()
    {
        base.EndInit();
        var vm = _viewModel ?? DataContext as TabsViewModel;
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
