using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using GameFinder.Objects;
using Wpf.Ui.Appearance;
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

