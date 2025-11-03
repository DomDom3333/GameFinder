using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Services;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel
{
    public TabsViewModel(ISteamGameService steamGameService, IUserConfigStore userConfig)
    {
        Home = new HomeTabViewModel(steamGameService, userConfig);
    }

    public HomeTabViewModel Home { get; }

    public Task InitializeAsync()
    {
        return Home.InitializeAsync();
    }

    public void UpdateSteamStatusFromSession(bool isConnected)
    {
        Home.UpdateSteamStatusFromSession(isConnected);
    }
}
