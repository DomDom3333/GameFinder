using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public sealed class UserEntryViewModel(string name, bool isAdmin, bool isCurrent) : INotifyPropertyChanged
{
    public string Name { get; } = name;

    public bool IsAdmin
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    } = isAdmin;

    public bool IsCurrent
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    } = isCurrent;

    public string DisplayName => $"{Name}{(IsCurrent ? " (You)" : string.Empty)}{(IsAdmin ? " (Admin)" : string.Empty)}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}