using System;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class SettingsTabViewModel
{
    public SettingsTabViewModel(HomeTabViewModel home)
    {
        Home = home ?? throw new ArgumentNullException(nameof(home));
    }

    public HomeTabViewModel Home { get; }
}
