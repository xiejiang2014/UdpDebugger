using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace UdpDebugger
{
    public partial class MainWindow : Window
    {
        private WindowNotificationManager? _manager;

        public MainWindow()
        {
            InitializeComponent();

            _autoUdpClient.DataReceived += _autoUdpClient_DataReceived;
            _autoUdpClient.ErrorChanged += AutoUdpClientErrorChanged;
        }


        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var topLevel = TopLevel.GetTopLevel(this);
            _manager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
        }


        private readonly AutoUdpClient _autoUdpClient = new();
        public           string        LocalIp    { get; set; } = "127.0.0.1";
        public           int           LocalPort  { get; set; } = 10000;
        public           string        RemoteIp   { get; set; } = "127.0.0.1";
        public           int           RemotePort { get; set; } = 10000;

        private bool _connected;

        public bool Connected
        {
            get => _connected;
            set
            {
                _connected = value;

                if (value)
                {
                    Connect();

                    TextBox_LocalIP.IsEnabled          = false;
                    NumericUpDown_LocalPort.IsEnabled  = false;
                    TextBox_RemoteIp.IsEnabled         = false;
                    NumericUpDown_RemotePort.IsEnabled = false;
                }
                else
                {
                    Disconnect();

                    TextBox_LocalIP.IsEnabled          = true;
                    NumericUpDown_LocalPort.IsEnabled  = true;
                    TextBox_RemoteIp.IsEnabled         = true;
                    NumericUpDown_RemotePort.IsEnabled = true;
                }
            }
        }


        public ObservableCollection<DataViewTypes> DataTypes { get; } = new ObservableCollection<DataViewTypes>()
                                                                        {
                                                                            DataViewTypes.Hex,
                                                                            DataViewTypes.Float
                                                                        };

        public DataViewTypes DataViewType { get; set; } = DataViewTypes.Hex;


        public ObservableCollection<string> ReceivedMessages { get; set; } = new();


        private void Connect()
        {
            try
            {
                _autoUdpClient.LocalIp    = LocalIp;
                _autoUdpClient.LocalPort  = LocalPort;
                _autoUdpClient.RemoteIp   = RemoteIp;
                _autoUdpClient.RemotePort = RemotePort;
                _autoUdpClient.Connect();
            }
            catch (Exception e)
            {
                _manager?.Show(new Notification("开启udp失败", e.Message, NotificationType.Error));
                Connected = false;
            }
        }

        private void _autoUdpClient_DataReceived(object? sender, byte[] receiveBytes)
        {
            if (DataViewType == DataViewTypes.Hex)
            {
                ReceivedMessages.Add(receiveBytes.BytesToString());
            }
            else if (DataViewType == DataViewTypes.Float)
            {
                ReceivedMessages.Add(string.Join(",", receiveBytes.BytesToFloatArray()));
            }

            Dispatcher.UIThread.Post(() => ListBoxReceivedMessages.ScrollIntoView(ReceivedMessages.Count - 1),
                                     DispatcherPriority.Background);
        }

        private void AutoUdpClientErrorChanged(object? sender, string e)
        {
            Dispatcher.UIThread.Post(() => TextBlockErrorMessage.Text = e);
        }

        private void Disconnect()
        {
            _autoUdpClient.Disconnect();
        }

        private void Button_Clear_OnClick(object? sender, RoutedEventArgs e)
        {
            ReceivedMessages.Clear();
        }

        private async void Button_Output_OnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);

            if (topLevel is null)
            {
                return;
            }

            var pickerFileType = new FilePickerFileType("文本文件")
                                 {
                                     Patterns = new[] { "*.txt" },
                                 };


            // Start async operation to open the dialog.
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                                                                          {
                                                                              SuggestedFileName = $"udp调试日志 {DateTime.Now:yyyy-MM-dd HH-mm-ss}",
                                                                              Title             = "导出为文件",
                                                                              FileTypeChoices   = new[] { pickerFileType }
                                                                          });

            if (file is not null)
            {
                // Open writing stream from the file.
                await using var stream       = await file.OpenWriteAsync();
                await using var streamWriter = new StreamWriter(stream);
                // Write some content to the file.

                for (var i = 0; i < ReceivedMessages.Count; i++)
                {
                    await streamWriter.WriteLineAsync(ReceivedMessages[i]);
                }
            }
        }

        private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: string text })
            {
                Clipboard?.SetTextAsync(text);
            }
        }

        #region 发送

        public ObservableCollection<string> SendMessages { get; set; } = new();

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            Send();
        }

        private Timer? _timer;

        private void CheckBox_KeepSending_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (CheckBox_KeepSending.IsChecked.GetValueOrDefault(false))
            {
                if (_timer == null)
                {
                    _timer         =  new Timer();
                    _timer.Elapsed += _timer_Elapsed;
                }

                _timer.Interval = (double)NumericUpDown_Intervals.Value!;
                _timer.Start();
            }
            else
            {
                _timer?.Stop();
            }
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Send();
        }

        private void Send()
        {
            var sendText = string.Empty;


            Dispatcher.UIThread.Invoke(() => sendText = TextBoxSend.Text,
                                       DispatcherPriority.Input);

            if (string.IsNullOrWhiteSpace(sendText))
            {
                return;
            }

            if (!_autoUdpClient.IsWorking)
            {
                return;
            }

            try
            {
                var sendBytes = Array.Empty<byte>();
                try
                {
                    sendBytes = sendText.HexStringToBytes();
                }
                catch (Exception e)
                {
                    sendText = "数据格式错误,无法将其转换为字节数组.";
                }

                if (sendBytes.Length > 0)
                {
                    _autoUdpClient?.SendData(sendBytes);
                }
                
                Dispatcher.UIThread.Post(() =>
                                         {
                                             //SendMessages.Add(sendText);
                                             //ListBoxSendMessages.ScrollIntoView(SendMessages.Count - 1);
                                         },
                                         DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.Post(() =>
                                         {
                                             //SendMessages.Add($"数据发送出错{e.Message}");
                                             //ListBoxSendMessages.ScrollIntoView(SendMessages.Count - 1);
                                         },
                                         DispatcherPriority.Background);
            }
        }

        #endregion
    }


    public enum DataViewTypes
    {
        Hex,
        Float
    }
}