using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace GameFinder;

public partial class SplashScreen : FluentWindow
{
    public SplashScreen()
    {
        InitializeComponent();
        Opacity = 0;
        Loaded += (_, _) =>
        {
            BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
        };
    }

    public async Task CloseWithFadeAsync()
    {
        var tcs = new TaskCompletionSource();
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        anim.Completed += (_, _) => tcs.SetResult();
        BeginAnimation(OpacityProperty, anim);
        await tcs.Task;
        Close();
    }
}
