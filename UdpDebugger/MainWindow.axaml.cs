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
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var topLevel = TopLevel.GetTopLevel(this);
            _manager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
        }


        private UdpClient?  _udpClient;
        private IPEndPoint? _ipEndPoint;

        public string Ip   { get; set; } = "127.0.0.1";
        public int    Port { get; set; } = 10000;

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

        public ObservableCollection<string> Messages { get; set; } = new();
        //  {
        //      "2a , 80 , bc , be , 57 , d4 , 35 , 42 , a8 , e3 , c6 , bf , b2 , f3 , 9a , be , cc , 0c , a6 , be , 97 , 5d , a7 , 40",
        //      "2i , 8i , 1l , be , 57 , d4 , 35 , 42 , a8 , e3 , c6 , bf , b2 , f3 , 9a , be , cc , 0c , a6 , be , 97 , 5d , a7 , 40",
        //  };

        private void Connect()
        {
            try
            {
                _udpClient  = new UdpClient(Port);
                _ipEndPoint = new IPEndPoint(IPAddress.Any, Port);

                _udpClient.BeginReceive(DataReceived, null);
            }
            catch (Exception e)
            {
                _manager?.Show(new Notification("开启udp失败", e.Message, NotificationType.Error));
                Connected = false;
            }
        }

        private void DataReceived(IAsyncResult ar)
        {
            if (_udpClient is null || Connected == false)
            {
                return;
            }

            var receiveBytes = _udpClient.EndReceive
                (
                 ar,
                 ref _ipEndPoint
                );


            _udpClient.BeginReceive(DataReceived, null);

            Messages.Add(receiveBytes.BytesToString());

            Dispatcher.UIThread.Post(() => ListBox_Messages.ScrollIntoView(Messages.Count - 1),
                                     DispatcherPriority.Background);
        }

        private void Disconnect()
        {
            if (_udpClient is null)
            {
                return;
            }

            try
            {
                _udpClient.Close();
                _udpClient = null;
            }
            catch (Exception e)
            {
            }
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
    }
}