using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Prism.Commands;
using XieJiang.CommonModule;
using XieJiang.CommonModule.Ava;

namespace UdpDebugger
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly AutoUdpClient _autoUdpClient = new();

        public MainWindowViewModel()
        {
            _autoUdpClient.DataReceived += AutoUdpClient_DataReceived;
            _autoUdpClient.ErrorChanged += AutoUdpClient_ErrorChanged;
        }

        public string ErrorMessage { get; set; }

        public IPAddress LocalIp   { get; set; } = IPAddress.Parse("127.0.0.1");
        public int       LocalPort  { get; set; } = 10000;
        public IPAddress RemoteIp   { get; set; } = IPAddress.Parse("127.0.0.1");
        public int       RemotePort { get; set; } = 10000;

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
                }
                else
                {
                    Disconnect();
                }

                OnPropertyChanged();
            }
        }


        public ObservableCollection<DataViewTypes> DataTypes { get; } =
        [
            DataViewTypes.Hex,
            DataViewTypes.Float
        ];

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
                _autoUdpClient.StartTryingConnect();
            }
            catch (Exception e)
            {
                Helper.Manager?.Show(new Notification("开启udp失败", e.Message, NotificationType.Error));
                Connected = false;
            }
        }

        private void AutoUdpClient_DataReceived(object? sender, byte[] receiveBytes)
        {
            if (DataViewType == DataViewTypes.Hex)
            {
                ReceivedMessages.Add(receiveBytes.BytesToString());
            }
            else if (DataViewType == DataViewTypes.Float)
            {
                ReceivedMessages.Add(string.Join(",", receiveBytes.BytesToFloatArray()));
            }
        }

        private void AutoUdpClient_ErrorChanged(object? sender, string e)
        {
            ErrorMessage = e;
        }

        private void Disconnect()
        {
            _autoUdpClient.Disconnect();
        }

        #region 清除

        private DelegateCommand? _clearCommand;
        public  DelegateCommand  ClearCommand => _clearCommand ??= new DelegateCommand(Clear);

        private void Clear()
        {
            ReceivedMessages.Clear();
        }

        #endregion

        #region 导出

        private DelegateCommand? _outputCommand;

        public DelegateCommand OutputCommand => _outputCommand ??= new DelegateCommand(async () => await Output());

        private async Task Output()
        {
            if (Helper.TopLevel is null)
            {
                return;
            }

            var pickerFileType = new FilePickerFileType("文本文件")
                                 {
                                     Patterns = new[] { "*.txt" },
                                 };


            // Start async operation to open the dialog.
            var file = await Helper.TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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

        #endregion

        //private DelegateCommand<object>? _copyClipboardCommand;
        //public  DelegateCommand<object>  CopyClipboardCommand => _copyClipboardCommand ??= new DelegateCommand<object>(CopyClipboard);

        //private void CopyClipboard(object p)
        //{
        //    if (p is string text)
        //    {
        //        Clipboard?.SetTextAsync(text);
        //    }
        //}


        #region 发送

        public ObservableCollection<string> SendMessages { get; set; } = new();

        public string SendingText { get; set; }

        private DelegateCommand? _sendCommand;
        public  DelegateCommand  SendCommand => _sendCommand ??= new DelegateCommand(Send);


        private Timer? _timer;


        public double KeepSendingIntervals { get; set; } = 10;

        private bool _keepSending;

        public bool KeepSending
        {
            get => _keepSending;
            set
            {
                _keepSending = value;

                if (_keepSending)
                {
                    if (_timer == null)
                    {
                        _timer         =  new Timer();
                        _timer.Elapsed += _timer_Elapsed;
                    }

                    _timer.Interval = KeepSendingIntervals;
                    _timer.Start();
                }
                else
                {
                    _timer?.Stop();
                }
            }
        }


        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Send();
        }

        private void Send()
        {
            var sendText = SendingText;


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

                SendMessages.Add(sendText);
            }
            catch (Exception e)
            {
                SendMessages.Add($"数据发送出错{e.Message}");
            }
        }

        #endregion


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}