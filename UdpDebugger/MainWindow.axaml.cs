using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XieJiang.CommonModule.Ava;

namespace UdpDebugger;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public MainWindow()
    {
        InitializeComponent();
        _mainWindowViewModel = new MainWindowViewModel();
        DataContext          = _mainWindowViewModel;

        _mainWindowViewModel.SendMessages.CollectionChanged     += SendMessages_CollectionChanged;
        _mainWindowViewModel.ReceivedMessages.CollectionChanged += ReceivedMessages_CollectionChanged;
    }


    private void SendMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => ListBoxSendMessages.ScrollIntoView(_mainWindowViewModel.SendMessages.Count - 1),
                                 DispatcherPriority.Background
                                );
    }

    private void ReceivedMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => ListBoxReceivedMessages.ScrollIntoView(_mainWindowViewModel.ReceivedMessages.Count - 1),
                                 DispatcherPriority.Background
                                );
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Helper.TopLevel = GetTopLevel(this);
        Helper.Manager  = new WindowNotificationManager(Helper.TopLevel) { MaxItems = 3 };
    }


    private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: string text })
        {
            Clipboard?.SetTextAsync(text);
        }
    }
}