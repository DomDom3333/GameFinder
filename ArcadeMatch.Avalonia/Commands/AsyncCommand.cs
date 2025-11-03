using System.Windows.Input;
using ArcadeMatch.Avalonia.Infrastructure;
using Avalonia.Threading; // added for UI thread marshaling

namespace ArcadeMatch.Avalonia.Commands;

public class AsyncCommand : ObservableObject, ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public AsyncCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            // Always marshal mutations + notifications to UI thread.
            if (!Dispatcher.UIThread.CheckAccess())
            {
                // Post rather than InvokeAsync to avoid deadlocks and because we don't need return value.
                Dispatcher.UIThread.Post(() => IsExecuting = value);
                return;
            }

            field = value;
            RaisePropertyChanged(); // notify binding system
            RaiseCanExecuteChanged(); // update buttons
        }
    }

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting)
        {
            return false;
        }

        return _canExecute?.Invoke(parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        // Removed ConfigureAwait(false) to ensure continuation resumes on UI thread.
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            IsExecuting = true;
            await _execute(parameter); // execution may hop threads; IsExecuting reset handles marshaling
        }
        finally
        {
            IsExecuting = false; // setter handles marshaling to UI thread
        }
    }

    public Task ExecuteAsync() => ExecuteAsync(null);

    public void RaiseCanExecuteChanged()
    {
        // Ensure event is raised on UI thread to avoid InvalidOperationException.
        if (Dispatcher.UIThread.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}