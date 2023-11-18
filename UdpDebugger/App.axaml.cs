using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace UdpDebugger
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            var fontFamily = new FontFamily(new Uri("avares://UdpDebugger/Assets/FZLanTingHeiS-L-GB.TTF"), "FZLanTingHeiS-L-GB");
            Resources.Add("FZLanTingHeiSLGB", fontFamily);


            //var fontFamily2 = new FontFamily(new Uri("avares://UdpDebugger/Assets/SourceHanSansCN-Normal.otf"), "SourceHanSansCN-Normal-Alphabetic");
            //Resources.Add("SourceHanSansCN-Normal", fontFamily2);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}