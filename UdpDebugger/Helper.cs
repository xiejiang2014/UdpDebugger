using Avalonia.Controls.Notifications;
using Avalonia.Controls;

namespace UdpDebugger
{
    internal static class Helper
    {
        public static TopLevel?                  TopLevel { get; set; }
        public static WindowNotificationManager? Manager  { get; set; }
    }
}