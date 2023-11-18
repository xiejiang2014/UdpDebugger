using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

            _autoUdpClient.DataReceived  += _autoUdpClient_DataReceived;
            _autoUdpClient.ErrorChanged += AutoUdpClientErrorChanged;
        }


        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var topLevel = TopLevel.GetTopLevel(this);
            _manager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
        }


        private AutoUdpClient _autoUdpClient = new();
        public  string        Ip   { get; set; } = "127.0.0.1";
        public  int           Port { get; set; } = 10000;

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

                    TextBox_IP.IsEnabled         = false;
                    NumericUpDown_Port.IsEnabled = false;
                }
                else
                {
                    Disconnect();

                    TextBox_IP.IsEnabled         = true;
                    NumericUpDown_Port.IsEnabled = true;
                }
            }
        }


        public ObservableCollection<DataViewTypes> DataTypes { get; } = new ObservableCollection<DataViewTypes>()
                                                                        {
                                                                            DataViewTypes.Hex,
                                                                            DataViewTypes.Float
                                                                        };

        public DataViewTypes DataViewType { get; set; } = DataViewTypes.Hex;


        public ObservableCollection<string> Messages { get; set; } = new();


        private void Connect()
        {
            try
            {
                _autoUdpClient.LocalIp   = Ip;
                _autoUdpClient.LocalPort = Port;
                _autoUdpClient.Start();
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
                Messages.Add(receiveBytes.BytesToString());
            }
            else if (DataViewType == DataViewTypes.Float)
            {
                Messages.Add(string.Join(",", receiveBytes.BytesToFloatArray()));
            }

            Dispatcher.UIThread.Post(() => ListBox_Messages.ScrollIntoView(Messages.Count - 1),
                                     DispatcherPriority.Background);
        }

        private void AutoUdpClientErrorChanged(object? sender, string e)
        {
            Dispatcher.UIThread.Post(() => TextBlockErrorMessage.Text = e);
        }

        private void Disconnect()
        {
            _autoUdpClient.Stop();
        }

        private void Button_Clear_OnClick(object? sender, RoutedEventArgs e)
        {
            Messages.Clear();
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

                for (var i = 0; i < Messages.Count; i++)
                {
                    await streamWriter.WriteLineAsync(Messages[i]);
                }
            }
        }

        private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem {DataContext: string text})
            {
                Clipboard?.SetTextAsync(text);
            }
        }
    }


    public enum DataViewTypes
    {
        Hex,
        Float
    }
}