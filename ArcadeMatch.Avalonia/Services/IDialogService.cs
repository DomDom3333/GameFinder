using Avalonia.Controls;

namespace ArcadeMatch.Avalonia.Services;

public interface IDialogService
{
    Task ShowMessageAsync(Window owner, string title, string message);
}
