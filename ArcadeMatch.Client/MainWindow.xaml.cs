using System.Windows;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;
using Wpf.Ui.Interop;

namespace GameFinder
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            TitleBar = AppTitleBar;
            ExtendsContentIntoTitleBar = true;
            Opacity = 0;
            Loaded += (_, _) =>
            {
                UnsafeNativeMethods.ExtendClientAreaIntoTitleBar(this);
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
            };
        }
    }
}

