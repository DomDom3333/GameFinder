using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using Avalonia.Media.Imaging;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public sealed class MatchResultItemViewModel : INotifyPropertyChanged
{
    public MatchResultItemViewModel(MatchedGame game)
    {
        Game = game;
        OpenSteamCommand = new RelayCommand(_ => OpenSteam());
    }

    public MatchedGame Game { get; }

    public ICommand OpenSteamCommand { get; }

    public Bitmap? Cover
    {
        get;
        set
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    private void OpenSteam()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Game.SteamUri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to open Steam page: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}