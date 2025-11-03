namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel
{
    public TabsViewModel(ArcadeMatch.Avalonia.Services.ISteamGameService steamGameService, ArcadeMatch.Avalonia.Services.IUserConfigStore userConfig)
    {
        Home = new HomeTabViewModel(steamGameService, userConfig);
    }

    public HomeTabViewModel Home { get; }

    public void UpdateSteamStatusFromSession(bool isConnected) => Home.UpdateSteamStatusFromSession(isConnected);
}
